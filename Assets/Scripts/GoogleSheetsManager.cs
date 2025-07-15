using UnityEngine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System;
using System.Net.Sockets;

public class GoogleSheetsManager : MonoBehaviour
{
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
    private static readonly string ApplicationName = "UnityRoomDatabase";
    private static readonly string SpreadsheetId = "1hHPRuVrfAgowkSuLAoVmxVBquJHFCBMF8zzt06Y0u1c"; // Thay bằng ID bảng tính thực tế
    private static readonly string SheetName = "Sheet1"; // Thay bằng tên sheet thực tế nếu khác
    private SheetsService sheetsService;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // Persist across scenes
        InitializeSheetsService();
    }

    private void InitializeSheetsService()
    {
        string tempPath = Path.Combine(Application.temporaryCachePath, "credentials.json");
        try
        {
            // Load credentials.json from Resources
            TextAsset credentialsAsset = Resources.Load<TextAsset>("credentials");
            if (credentialsAsset == null)
            {
                Debug.LogError("Credentials file not found in Assets/Resources/credentials.json");
                return;
            }

            // Write the JSON content to a temporary file
            File.WriteAllText(tempPath, credentialsAsset.text);
            if (!File.Exists(tempPath))
            {
                Debug.LogError($"Failed to create temporary credentials file at {tempPath}");
                return;
            }

            GoogleCredential credential;
            using (var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            Debug.Log("Google Sheets API initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Google Sheets API: {e.Message}");
        }
        finally
        {
            // Always attempt to delete the temporary file
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to delete temporary credentials file: {e.Message}");
            }
        }
    }

    // Append room code and IP address to the Google Sheet
    public async Task<bool> AppendRoomData(string roomCode, string ipAddress)
    {
        if (sheetsService == null)
        {
            Debug.LogError("SheetsService is not initialized.");
            return false;
        }

        Debug.Log($"Attempting to append roomCode: {roomCode}, ipAddress: {ipAddress} to spreadsheet ID: {SpreadsheetId}, sheet: {SheetName}");

        try
        {
            var range = $"{SheetName}!A:B";
            var valueRange = new ValueRange
            {
                Values = new[] { new object[] { roomCode, ipAddress } }
            };

            var appendRequest = sheetsService.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var response = await appendRequest.ExecuteAsync();
            Debug.Log($"Successfully appended room code {roomCode} with IP {ipAddress} to Google Sheet.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to append room data: {e.Message}");
            return false;
        }
    }

    // Retrieve IP address for a given room code
    public async Task<string> GetIpAddressByRoomCode(string roomCode)
    {
        if (sheetsService == null)
        {
            Debug.LogError("SheetsService is not initialized.");
            return null;
        }

        Debug.Log($"Retrieving IP address for room code: {roomCode} from spreadsheet ID: {SpreadsheetId}, sheet: {SheetName}");

        try
        {
            var range = $"{SheetName}!A:B";
            var request = sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count >= 2 && row[0].ToString() == roomCode)
                    {
                        Debug.Log($"Found IP address {row[1]} for room code {roomCode}");
                        return row[1].ToString();
                    }
                }
                Debug.LogWarning($"No IP address found for room code {roomCode}");
                return null;
            }
            else
            {
                Debug.LogWarning("No data found in Google Sheet.");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve IP address for room code {roomCode}: {e.Message}");
            return null;
        }
    }

    // Delete room data from Google Sheet by room code
    public async Task<bool> DeleteRoomData(string roomCode)
    {
        if (sheetsService == null)
        {
            Debug.LogError("SheetsService is not initialized.");
            return false;
        }

        Debug.Log($"Attempting to delete room code: {roomCode} from spreadsheet ID: {SpreadsheetId}, sheet: {SheetName}");

        try
        {
            // Get all data to find the row with the room code
            var range = $"{SheetName}!A:B";
            var request = sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                int rowIndex = -1;
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].Count >= 1 && values[i][0].ToString() == roomCode)
                    {
                        rowIndex = i + 1; // Google Sheets row index is 1-based
                        break;
                    }
                }

                if (rowIndex == -1)
                {
                    Debug.LogWarning($"No row found for room code {roomCode}");
                    return false;
                }

                // Delete the row
                var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new[]
                    {
                        new Request
                        {
                            DeleteDimension = new DeleteDimensionRequest
                            {
                                Range = new DimensionRange
                                {
                                    SheetId = GetSheetId(),
                                    Dimension = "ROWS",
                                    StartIndex = rowIndex - 1,
                                    EndIndex = rowIndex
                                }
                            }
                        }
                    }
                };

                var deleteRequest = sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, SpreadsheetId);
                await deleteRequest.ExecuteAsync();
                Debug.Log($"Successfully deleted room code {roomCode} from Google Sheet at row {rowIndex}");
                return true;
            }
            else
            {
                Debug.LogWarning("No data found in Google Sheet.");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete room code {roomCode}: {e.Message}");
            return false;
        }
    }

    // Helper method to get SheetId
    private int GetSheetId()
    {
        try
        {
            var request = sheetsService.Spreadsheets.Get(SpreadsheetId);
            var response = request.Execute();
            var sheet = response.Sheets.FirstOrDefault(s => s.Properties.Title == SheetName);
            if (sheet != null)
            {
                return sheet.Properties.SheetId.Value;
            }
            Debug.LogError($"Sheet {SheetName} not found in spreadsheet.");
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get SheetId for {SheetName}: {e.Message}");
            return 0;
        }
    }

    // Get the local machine's IP address
    public static string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                {
                    return ip.ToString();
                }
            }
            Debug.LogWarning("No IPv4 address found. Falling back to localhost.");
            return "localhost";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get local IP address: {e.Message}");
            return "localhost";
        }
    }
}