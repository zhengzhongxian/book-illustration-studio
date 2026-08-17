using Studio.Api.Domain.Enums;

namespace Studio.Api.Application.DTOs;

public record RunStepRequest(
    string? CustomStyle = null
);

public record StepExecutionResultDto(
    string ProjectId,
    StepKey Step,
    ProjectStatus Status,
    StepState StepState,
    string? Message = null
);
