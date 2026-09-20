namespace AssetManagement.Domain.Inventory;

public enum MovementType
{
    Assignment,
    AssignmentReturn,
    Loan,
    LoanReturn,
    Relocation,
    CrossCompanyTransferOut,
    CrossCompanyTransferIn,
}
