using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Outcome of a payment recording.</summary>
public class PaymentResult
{
    /// <summary>True when the payment was stored.</summary>
    public bool Success { get; init; }

    /// <summary>Error message shown to the user when the payment could not be stored.</summary>
    public string? Error { get; init; }

    /// <summary>Number of the generated receipt.</summary>
    public string? ReceiptNumber { get; init; }

    /// <summary>Amount still outstanding on the bill.</summary>
    public decimal AmountDue { get; init; }

    /// <summary>Payment state of the bill after the receipt.</summary>
    public BillStatus Status { get; init; }
}

/// <summary>
/// Records the cash receipts of the vendor dashboard. Only cash on delivery / pay at counter
/// payments exist, so no gateway integration is needed.
/// </summary>
public class PaymentService
{
    /// <summary>Database context used to store the receipts.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>Service that hands out the serial numbers of the receipt numbers.</summary>
    private readonly SequenceService _sequence;

    /// <summary>Billing calculator used to resolve the payment state of the bill.</summary>
    private readonly BillingService _billing;

    /// <summary>Creates the service with the injected dependencies.</summary>
    public PaymentService(ApplicationDbContext db, SequenceService sequence, BillingService billing)
    {
        _db = db;
        _sequence = sequence;
        _billing = billing;
    }

    /// <summary>
    /// Records a cash receipt against a bill and updates the payment state of the bill.
    /// A partial payment keeps the bill in the "Due" state with a smaller outstanding amount.
    /// </summary>
    /// <param name="bill">Bill the receipt is recorded against.</param>
    /// <param name="amount">Amount received in Rupees.</param>
    /// <param name="userId">User account that received the cash.</param>
    /// <param name="userName">Display name of the person that received the cash.</param>
    /// <param name="remarks">Remarks entered on the receipt.</param>
    /// <param name="paidOn">Date the cash was received.</param>
    public async Task<PaymentResult> RecordPaymentAsync(Bill bill, decimal amount, string? userId, string? userName,
        string? remarks = null, DateTime? paidOn = null, CancellationToken cancellationToken = default)
    {
        if (amount <= 0m)
        {
            return new PaymentResult { Success = false, Error = "The received amount has to be greater than zero." };
        }

        var outstanding = bill.AmountDue;

        // Overpayment is not possible - the vendor can only collect what is still due.
        if (amount > outstanding + 0.009m)
        {
            return new PaymentResult
            {
                Success = false,
                Error = $"The amount is higher than the outstanding amount of {outstanding:N2}."
            };
        }

        var receiptSerial = await _sequence.NextSerialAsync("RECEIPT", cancellationToken);
        var receiptNumber = $"RCP-{receiptSerial.ToString("D6")}";

        var payment = new Payment
        {
            BillId = bill.Id,
            ConnectionId = bill.ConnectionId,
            ReceiptNumber = receiptNumber,
            Amount = Math.Round(amount, 2),
            Method = PaymentMethod.Cash,
            PaidOn = paidOn ?? DateTime.Now,
            ReceivedByUserId = userId,
            ReceivedByName = userName,
            Remarks = remarks
        };

        _db.Payments.Add(payment);

        // The payment state of the bill is recalculated from the totals.
        var (status, paid, due) = _billing.ResolvePaymentState(bill.TotalAmount, bill.AmountPaid + amount, bill.AmountDue - amount);

        bill.AmountPaid = paid;
        bill.AmountDue = due;
        bill.Status = status;

        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentResult
        {
            Success = true,
            ReceiptNumber = receiptNumber,
            AmountDue = due,
            Status = status
        };
    }

    /// <summary>
    /// Marks the whole outstanding amount of a bill as paid (one click action of the vendor).
    /// </summary>
    public Task<PaymentResult> MarkAsPaidAsync(Bill bill, string? userId, string? userName, string? remarks = null,
        CancellationToken cancellationToken = default)
        => RecordPaymentAsync(bill, bill.AmountDue, userId, userName, remarks ?? "Full payment received at the counter.", null, cancellationToken);
}
