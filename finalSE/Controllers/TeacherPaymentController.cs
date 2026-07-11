using finalSE.Models;
using finalSE.Service.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeacherPaymentController : Controller
    {
        private readonly MyDBContext _context;
        private readonly BkashPaymentService _bkashService;

        public TeacherPaymentController(MyDBContext context, BkashPaymentService bkashService)
        {
            _context = context;
            _bkashService = bkashService;
        }

        // ================= PAYMENT MANAGEMENT PAGE =================
        public async Task<IActionResult> Index()
        {
            var teachers = await _context.Teachers
                .Include(t => t.Department)
                .OrderBy(t => t.Department.DepartmentName)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var payments = await _context.TeacherPayments
                .Include(p => p.Teacher)
                .ThenInclude(t => t.Department)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.Teachers = new SelectList(teachers, "Id", "Name");
            ViewBag.TeachersList = teachers;
            ViewBag.Payments = payments;

            return View();
        }

        // ================= INITIATE BKASH PAYMENT =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePayment(int teacherId, decimal amount, string month)
        {
            // Validate inputs
            if (teacherId <= 0 || amount <= 0 || string.IsNullOrWhiteSpace(month))
            {
                TempData["ErrorMessage"] = "Invalid payment details. Please fill all fields correctly.";
                return RedirectToAction(nameof(Index));
            }

            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Teacher not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check for duplicate payment for the same month
            var existingPayment = await _context.TeacherPayments
                .FirstOrDefaultAsync(p => p.TeacherID == teacherId && p.Month == month && p.Status == "Paid");
            if (existingPayment != null)
            {
                TempData["ErrorMessage"] = $"Payment for {teacher.Name} for {month} is already completed (TrxID: {existingPayment.TransactionID}).";
                return RedirectToAction(nameof(Index));
            }

            // Create a Pending payment record in DB
            var payment = new TeacherPayment
            {
                TeacherID = teacherId,
                Amount = amount,
                Month = month,
                Status = "Pending",
                PaymentDate = DateTime.Now
            };

            _context.TeacherPayments.Add(payment);
            await _context.SaveChangesAsync();

            // Generate unique invoice number
            var invoiceNumber = $"TCHR-{payment.PaymentID}-{DateTime.Now:yyyyMMddHHmmss}";

            // Build callback URL
            var callbackUrl = Url.Action("BkashCallback", "TeacherPayment", null, Request.Scheme);

            // Call bKash Create Payment API
            var bkashResponse = await _bkashService.CreatePaymentAsync(amount, invoiceNumber, callbackUrl!);

            if (bkashResponse == null || string.IsNullOrEmpty(bkashResponse.BkashURL))
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "Failed to initiate bKash payment. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            // Store bKash PaymentID in our record
            payment.BkashPaymentID = bkashResponse.PaymentID;
            await _context.SaveChangesAsync();

            // Store payment ID in session for callback reference
            HttpContext.Session.SetInt32("PendingPaymentId", payment.PaymentID);

            // Redirect to bKash checkout page
            return Redirect(bkashResponse.BkashURL);
        }

        // ================= BKASH CALLBACK =================
        [HttpGet]
        public async Task<IActionResult> BkashCallback(string paymentID, string status)
        {
            var pendingPaymentId = HttpContext.Session.GetInt32("PendingPaymentId");

            TeacherPayment? payment = null;

            if (pendingPaymentId.HasValue)
            {
                payment = await _context.TeacherPayments.FindAsync(pendingPaymentId.Value);
            }

            // Fallback: find by bKash PaymentID
            if (payment == null && !string.IsNullOrEmpty(paymentID))
            {
                payment = await _context.TeacherPayments
                    .FirstOrDefaultAsync(p => p.BkashPaymentID == paymentID);
            }

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment record not found. Please contact support.";
                return RedirectToAction(nameof(Index));
            }

            // Handle cancellation / failure from user side
            if (status != null && (status.ToLower() == "cancel" || status.ToLower() == "failure"))
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("PendingPaymentId");

                TempData["ErrorMessage"] = $"bKash payment was {status}. No charges applied.";
                return RedirectToAction(nameof(Index));
            }

            // Execute payment via bKash API
            if (!string.IsNullOrEmpty(paymentID))
            {
                var executeResult = await _bkashService.ExecutePaymentAsync(paymentID);

                if (executeResult != null && executeResult.TransactionStatus == "Completed")
                {
                    payment.TransactionID = executeResult.TrxID;
                    payment.BkashPaymentID = executeResult.PaymentID;
                    payment.Status = "Paid";
                    payment.PaymentDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    HttpContext.Session.Remove("PendingPaymentId");

                    TempData["SuccessMessage"] = $"Payment of ৳{payment.Amount:N2} for {payment.Month} completed successfully! TrxID: {executeResult.TrxID}";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    payment.Status = "Failed";
                    await _context.SaveChangesAsync();
                    HttpContext.Session.Remove("PendingPaymentId");

                    var errorMsg = executeResult?.StatusMessage ?? "Unknown error";
                    TempData["ErrorMessage"] = $"bKash payment execution failed: {errorMsg}";
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["ErrorMessage"] = "Invalid callback received from bKash.";
            return RedirectToAction(nameof(Index));
        }

        // ================= PAYMENT HISTORY (ADMIN VIEW) =================
        public async Task<IActionResult> PaymentHistory(int? teacherId, string? month)
        {
            var query = _context.TeacherPayments
                .Include(p => p.Teacher)
                .ThenInclude(t => t.Department)
                .AsQueryable();

            if (teacherId.HasValue && teacherId > 0)
            {
                query = query.Where(p => p.TeacherID == teacherId.Value);
            }

            if (!string.IsNullOrWhiteSpace(month))
            {
                query = query.Where(p => p.Month == month);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var teachers = await _context.Teachers
                .Include(t => t.Department)
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.Teachers = new SelectList(teachers, "Id", "Name", teacherId);
            ViewBag.SelectedTeacherId = teacherId;
            ViewBag.SelectedMonth = month;

            return View(payments);
        }

        // ================= DELETE PAYMENT (ADMIN ONLY) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var payment = await _context.TeacherPayments.FindAsync(id);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment record not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.TeacherPayments.Remove(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
