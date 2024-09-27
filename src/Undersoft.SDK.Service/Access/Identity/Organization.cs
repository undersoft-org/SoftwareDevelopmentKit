using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Data.Model.Attributes;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Access.Identity;

[ViewModel(
    Height = "550px",
    Width = "350px",
    Validator = "OrganizationValidator",
    SearchMembers = ["OrganizationIndustry", "OrganizationName", "PositionInOrganization"]
)]
public class Organization : DataObject, IOrganization
{
    [ViewRubric(
        "Logo",
        Size = 8,
        ImageMode = ImageMode.Regular,
        Width = "30px",
        Height = "30px",
        DataTarget = "OrganizationImageData"
    )]
    [ViewImage(ViewImageMode.Regular, "30px", "30px")]
    [FileRubric(FileRubricType.Property, "OrganizationImageData")]
    public string OrganizationImage { get; set; }

    [DisplayRubric("Industry")]
    public string OrganizationIndustry { get; set; }

    [DisplayRubric("Short name")]
    public string OrganizationName { get; set; }

    [DisplayRubric("Full name")]
    public string OrganizationFullName { get; set; }

    [DisplayRubric("Position")]
    public string PositionInOrganization { get; set; }

    [DisplayRubric("Websites")]
    public string OrganizationWebsites { get; set; }

    public byte[] OrganizationImageData { get; set; }

    public OrganizationSize OrganizationSize { get; set; }
}
