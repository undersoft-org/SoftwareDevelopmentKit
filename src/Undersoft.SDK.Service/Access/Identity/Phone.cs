// *************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SCC.Service
// *************************************************

using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Data.Contract;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Access.Identity
{
    public class Phone : DataObject, IContract
    {
        [VisibleRubric]
        public PhoneType PhoneType { get; set; }

        [VisibleRubric]
        public string PhoneNumber { get; set; }
    }
}