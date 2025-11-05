using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;
using HospitalAppointmentApi.Models;
using System.Threading;

namespace HospitalAppointmentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly string filePath = Path.Combine(Directory.GetCurrentDirectory(), "appointments.xlsx");

        // -----------------------------
        //  POST: api/appointment/book
        // -----------------------------
        [HttpPost("book")]
        public IActionResult BookAppointment([FromBody] Appointment appointment)
        {
            try
            {
                // Ensure main file exists
                if (!System.IO.File.Exists(filePath))
                {
                    using var workbook = new XLWorkbook();
                    var sheet = workbook.Worksheets.Add("Appointments");
                    sheet.Cell(1, 1).Value = "Name";
                    sheet.Cell(1, 2).Value = "Contact";
                    sheet.Cell(1, 3).Value = "Gender";
                    sheet.Cell(1, 4).Value = "AppointmentTime";
                    sheet.Cell(1, 5).Value = "Problem";
                    sheet.Cell(1, 6).Value = "Status";
                    workbook.SaveAs(filePath);
                }

                // Create a temp file to avoid "file in use" errors
                var tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appointments_temp.xlsx");

                // Load from original file into temp file
                using (var workbook = new XLWorkbook(filePath))
                {
                    var sheet = workbook.Worksheet("Appointments");
                    var nextRow = sheet.LastRowUsed()?.RowNumber() + 1 ?? 2;

                    sheet.Cell(nextRow, 1).Value = appointment.Name;
                    sheet.Cell(nextRow, 2).Value = appointment.Contact;
                    sheet.Cell(nextRow, 3).Value = appointment.Gender;
                    sheet.Cell(nextRow, 4).Value = appointment.AppointmentTime.ToString("g");
                    sheet.Cell(nextRow, 5).Value = appointment.Problem;
                    sheet.Cell(nextRow, 6).Value = appointment.Status ?? "Waiting";

                    workbook.SaveAs(tempFilePath);
                }

                // Replace main file safely (retry if locked)
                TryReplaceFile(tempFilePath, filePath);

                return Ok(new { message = "Appointment booked successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save appointment: {ex.Message}" });
            }
        }

        // -----------------------------
        //  GET: api/appointment/all
        // -----------------------------
        [HttpGet("all")]
        public IActionResult GetAppointments()
        {
            try
            {
                var list = new List<Appointment>();
                if (!System.IO.File.Exists(filePath))
                    return Ok(list);

                using (var workbook = new XLWorkbook(filePath))
                {
                    var sheet = workbook.Worksheet("Appointments");
                    foreach (var row in sheet.RowsUsed().Skip(1)) // skip header
                    {
                        var record = new Appointment
                        {
                            Name = row.Cell(1).GetString(),
                            Contact = row.Cell(2).GetString(),
                            Gender = row.Cell(3).GetString(),
                            AppointmentTime = DateTime.TryParse(row.Cell(4).GetString(), out var dt) ? dt : DateTime.MinValue,
                            Problem = row.Cell(5).GetString(),
                            Status = row.Cell(6).GetString()
                        };
                        list.Add(record);
                    }
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to load appointments: {ex.Message}" });
            }
        }

        // -----------------------------
        //  Helper: Replace file safely
        // -----------------------------
        private void TryReplaceFile(string source, string target)
        {
            const int maxRetries = 5;
            const int delayBetweenRetries = 2000; // 2 seconds

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // Try overwriting target
                    System.IO.File.Copy(source, target, true);
                    System.IO.File.Delete(source);
                    return;
                }
                catch (IOException)
                {
                    // File is probably open in Excel
                    Thread.Sleep(delayBetweenRetries);
                }
            }

            // Clean up temp file if it couldn’t replace
            if (System.IO.File.Exists(source))
                System.IO.File.Delete(source);

            throw new IOException("Could not update Excel file because it is open or locked. Please close the file and try again.");
        }
    }
}
