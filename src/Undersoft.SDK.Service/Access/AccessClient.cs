using Undersoft.SDK.Service.Data.Client;

// *************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.AMS.Service
// *************************************************

namespace Undersoft.SDK.Service.Access
{
    public class AccessClient(Uri uri) : DataClient<IAccountStore>(uri) { }
}
