using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Data.Model.Attributes;
using Undersoft.SDK.Service.Data.Object;
using Undersoft.SDK.Service.Operation;

namespace Undersoft.SDK.Service.Access.Identity;

[Validator("OrganizationValidator")]
[OpenSearch("OrganizationIndustry", "OrganizationName", "PositionInOrganization")]
[ViewSize("350px", "550px")]
public class Organization : DataObject, IOrganization
{
    [DisplayRubric("Image")]
    [ViewImage(ViewImageMode.Regular, "30px", "30px")]
    [FileRubric(FileRubricType.Property, "OrganizationImageData")]
    public string OrganizationImage { get; set; }

    [DisplayRubric("Industry")]
    public string OrganizationIndustry { get; set; }

    [DisplayRubric("Name")]
    public string OrganizationName { get; set; }

    [DisplayRubric("Full name")]
    public string OrganizationFullName { get; set; }

    [VisibleRubric]
    [DisplayRubric("Email")]
    public string OrganizationEmail { get; set; }

    [VisibleRubric]
    [DisplayRubric("Phone number")]
    public string OrganizationPhoneNumber { get; set; }

    [DisplayRubric("Position")]
    public string PositionInOrganization { get; set; }

    [DisplayRubric("Websites")]
    public string OrganizationWebsites { get; set; }

    public byte[] OrganizationImageData { get; set; }
    
    [DisplayRubric("Size")]
    public OrganizationSize OrganizationSize { get; set; }
}
