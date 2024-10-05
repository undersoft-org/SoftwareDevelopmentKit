using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Access.Licensing;

public class Subscription : DataObject, ISubscription
{    
    [RubricSize(64)]
    [DisplayRubric("Name")]
    public string SubscriptionName { get; set; }
 
    [RubricSize(128)]
    [DisplayRubric("Description")]
    public string SubscriptionDescription { get; set; }
    
    [DisplayRubric("Period")]
    public double SubscriptionPeriod { get; set; }
    
    [DisplayRubric("Expires date")]
    public DateTime SubscriptionExpireDate { get; set; } = DateTime.Parse("01.01.1990");
    
    [DisplayRubric("Quantity")]
    public double SubscriptionQuantity { get; set; }
    
    [DisplayRubric("Amount")]
    public double SubscriptionValue { get; set; }

    [RubricSize(4)]
    [DisplayRubric("Currency")]
    public string SubscriptionCurrency { get; set; }

    [RubricSize(16)]
    [DisplayRubric("Status")]
    public string SubscriptionStatus { get; set; }

    public string SubscriptionToken { get; set; }
}


