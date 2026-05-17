using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;

namespace BenServicesPlatform.Api.Mapping;

public static class EntityMappers
{
    public static ProviderDto ToDto(this ProviderEntity entity)
    {
        return new ProviderDto
        {
            Id = entity.Id,
            FullName = entity.FullName,
            BusinessName = entity.BusinessName,
            Phone = entity.Phone,
            Email = entity.Email,
            ServiceType = entity.ServiceType,
            ServicesOffered = JsonArrayMapper.Deserialize(entity.ServicesOfferedJson),
            City = entity.City,
            State = entity.State,
            ZipCodes = JsonArrayMapper.Deserialize(entity.ZipCodesJson),
            Region = entity.Region,
            EmergencyService = entity.EmergencyService,
            Availability = entity.Availability,
            WorkingHours = entity.WorkingHours,
            VerificationStatus = entity.VerificationStatus,
            IsActive = entity.IsActive,
            Source = entity.Source,
            YearsOfExperience = entity.YearsOfExperience,
            Notes = entity.Notes,
            AdminComments = entity.AdminComments,
            HasW9File = !string.IsNullOrWhiteSpace(entity.W9FilePath),
            HasCoiFile = !string.IsNullOrWhiteSpace(entity.CoiFilePath),
            W9UploadedAt = entity.W9UploadedAt,
            CoiUploadedAt = entity.CoiUploadedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            VerifiedAt = entity.VerifiedAt
        };
    }

    public static void ApplyUpdate(this ProviderEntity entity, IProviderUpsertPayload request)
    {
        entity.FullName = request.FullName.Trim();
        entity.BusinessName = request.BusinessName.Trim();
        entity.Phone = request.Phone.Trim();
        entity.Email = request.Email.Trim();
        entity.ServiceType = request.ServiceType;
        entity.ServicesOfferedJson = JsonArrayMapper.Serialize(request.ServicesOffered);
        entity.City = request.City.Trim();
        entity.State = request.State.Trim();
        entity.ZipCodesJson = JsonArrayMapper.Serialize(request.ZipCodes);
        entity.Region = request.Region.Trim();
        entity.EmergencyService = request.EmergencyService;
        entity.Availability = request.Availability.Trim();
        entity.WorkingHours = request.WorkingHours.Trim();
        entity.VerificationStatus = request.VerificationStatus;
        entity.IsActive = request.IsActive;
        entity.Source = request.Source;
        entity.YearsOfExperience = request.YearsOfExperience;
        entity.Notes = request.Notes?.Trim();
        entity.AdminComments = request.AdminComments?.Trim();
        entity.VerifiedAt = request.VerifiedAt;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public static ProviderApplicationDto ToDto(this ProviderApplicationEntity entity)
    {
        return new ProviderApplicationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName = entity.FullName,
            BusinessName = entity.BusinessName,
            Phone = entity.Phone,
            Email = entity.Email,
            ServiceType = entity.ServiceType,
            ServicesOffered = JsonArrayMapper.Deserialize(entity.ServicesOfferedJson),
            CitiesCovered = JsonArrayMapper.Deserialize(entity.CitiesCoveredJson),
            City = entity.City,
            State = entity.State,
            ZipCodes = JsonArrayMapper.Deserialize(entity.ZipCodesJson),
            YearsOfExperience = entity.YearsOfExperience,
            EmergencyService = entity.EmergencyService,
            WorkingHours = entity.WorkingHours,
            Message = entity.Message,
            Source = entity.Source,
            Status = entity.Status,
            AdminNotes = entity.AdminNotes,
            MissingInfoReason = entity.MissingInfoReason,
            RejectionReason = entity.RejectionReason,
            VerificationNotes = entity.VerificationNotes,
            ConvertedProviderId = entity.ConvertedProviderId,
            SubmittedAt = entity.SubmittedAt,
            UpdatedAt = entity.UpdatedAt,
            ReviewedAt = entity.ReviewedAt,
            VerifiedAt = entity.VerifiedAt,
            RejectedAt = entity.RejectedAt,
            LicenseFileName = entity.LicenseFileName,
            LicenseFileUrl = entity.LicenseFileUrl,
            InsuranceFileUrl = entity.InsuranceFileUrl,
            W9FileUrl = entity.W9FileUrl
        };
    }

    public static ProviderApplicationEntity ToEntity(this ProviderApplicationCreateRequest request)
    {
        var now = DateTime.UtcNow;

        return new ProviderApplicationEntity
        {
            FullName = request.FullName.Trim(),
            BusinessName = request.BusinessName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            ServiceType = request.ServiceType,
            ServicesOfferedJson = JsonArrayMapper.Serialize(request.ServicesOffered),
            CitiesCoveredJson = JsonArrayMapper.Serialize(request.CitiesCovered),
            City = request.City.Trim(),
            State = request.State.Trim(),
            ZipCodesJson = JsonArrayMapper.Serialize(request.ZipCodes),
            YearsOfExperience = request.YearsOfExperience,
            EmergencyService = request.EmergencyService,
            WorkingHours = request.WorkingHours.Trim(),
            Message = request.Message.Trim(),
            Source = "Form",
            Status = ProviderApplicationStatus.Pending,
            SubmittedAt = now,
            UpdatedAt = now,
            LicenseFileName = string.IsNullOrWhiteSpace(request.LicenseFileName) ? null : request.LicenseFileName.Trim()
        };
    }

    public static AdminResponseDto ToDto(this AdminEntity entity)
    {
        return new AdminResponseDto
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Email = entity.Email,
            Username = entity.Username,
            Role = entity.Role,
            IsActive = entity.IsActive,
            MustChangePassword = entity.MustChangePassword,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedByAdminId = entity.CreatedByAdminId
        };
    }

    public static ClientServiceRequestDto ToDto(this ClientServiceRequestEntity entity)
    {
        return new ClientServiceRequestDto
        {
            Id = entity.Id,
            ClientType = entity.ClientType,
            CompanyName = entity.CompanyName,
            ContactName = entity.ContactName,
            Phone = entity.Phone,
            Email = entity.Email,
            ServiceCategory = entity.ServiceCategory,
            Urgency = entity.Urgency,
            Address = entity.Address,
            City = entity.City,
            State = entity.State,
            ZipCode = entity.ZipCode,
            Description = entity.Description,
            PreferredDateTime = entity.PreferredDateTime,
            Status = entity.Status,
            Source = entity.Source,
            AdminNotes = entity.AdminNotes,
            PhotoFileUrl = entity.PhotoFileUrl,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
