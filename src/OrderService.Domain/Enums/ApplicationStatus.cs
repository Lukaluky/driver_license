namespace OrderService.Domain.Enums;

public enum ApplicationStatus
{
    Pending = 1,
    ExternalChecksInProgress = 2,
    ExternalChecksPassed = 3,
    ExternalChecksFailed = 4,
    AssignedToInspector = 5,
    Approved = 6,
    Rejected = 7,
    Printed = 8
}
