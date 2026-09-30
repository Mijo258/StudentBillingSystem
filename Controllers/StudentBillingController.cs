using Microsoft.AspNetCore.Mvc;
using StudentApi.Models;
using StudentApi.Services;

namespace StudentApi.Controllers;

/// <summary>
/// Exposes the student billing operations over HTTP.
///
/// This controller contains no business logic: it validates nothing about the
/// domain, owns no data, and simply delegates to <see cref="IStudentBillingService"/>
/// and translates the outcome into a status code.
/// </summary>
[ApiController]
[Route("api/students")]
[Produces("application/json")]
public class StudentBillingController : ControllerBase
{
    private readonly IStudentBillingService _billingService;

    // The service is supplied by the DI container (registered in Program.cs).
    public StudentBillingController(IStudentBillingService billingService)
    {
        _billingService = billingService;
    }

    /// <summary>
    /// GET /api/students/{studentNo}/installments
    /// Returns every instalment for the given student.
    /// </summary>
    /// <response code="200">The student's instalments, ordered by instalment number.</response>
    /// <response code="404">No instalments exist for that student number.</response>
    [HttpGet("{studentNo}/installments")]
    [ProducesResponseType(typeof(IReadOnlyList<Installment>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<Installment>> GetStudentInstalmentInfo([FromRoute] string studentNo)
    {
        var installments = _billingService.GetInstallmentsByStudentNo(studentNo);

        if (installments is null)
        {
            return NotFound(BuildProblem(
                StatusCodes.Status404NotFound,
                "Student not found",
                $"No installments exist for student '{studentNo}'."));
        }

        return Ok(installments);
    }

    /// <summary>
    /// PUT /api/students/{studentNo}/installments/{installmentNo}/pay
    /// Marks the instalment as "Paid" and stamps it with today's date.
    /// </summary>
    /// <response code="200">The instalment was paid; the updated resource is returned.</response>
    /// <response code="400">The instalment has already been paid.</response>
    /// <response code="404">The student, or the instalment, does not exist.</response>
    [HttpPut("{studentNo}/installments/{installmentNo}/pay")]
    [ProducesResponseType(typeof(Installment), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<Installment> PayStudentInstallment(
        [FromRoute] string studentNo,
        [FromRoute] int installmentNo)
    {
        var result = _billingService.PayInstallment(studentNo, installmentNo);

        switch (result)
        {
            case PayInstallmentResult.Success:
                // Re-read the (now updated) instalment so the client sees the new state
                // without having to make a second call.
                var paidInstallment = _billingService
                    .GetInstallmentsByStudentNo(studentNo)?
                    .FirstOrDefault(i => i.InstallmentNo == installmentNo);

                return Ok(paidInstallment);

            case PayInstallmentResult.AlreadyPaid:
                return BadRequest(BuildProblem(
                    StatusCodes.Status400BadRequest,
                    "Installment already paid",
                    $"Installment {installmentNo} for student '{studentNo}' has already been paid."));

            case PayInstallmentResult.StudentNotFound:
                return NotFound(BuildProblem(
                    StatusCodes.Status404NotFound,
                    "Student not found",
                    $"No installments exist for student '{studentNo}'."));

            case PayInstallmentResult.InstallmentNotFound:
                return NotFound(BuildProblem(
                    StatusCodes.Status404NotFound,
                    "Installment not found",
                    $"Student '{studentNo}' has no installment number {installmentNo}."));

            default:
                // Defensive: a new enum member added without a case here should fail loudly,
                // not silently report success.
                return StatusCode(StatusCodes.Status500InternalServerError, BuildProblem(
                    StatusCodes.Status500InternalServerError,
                    "Unhandled billing outcome",
                    $"The billing service returned an unsupported result: {result}."));
        }
    }

    /// <summary>Builds a consistent RFC 7807 error payload for failed requests.</summary>
    private static ProblemDetails BuildProblem(int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
    }
}
