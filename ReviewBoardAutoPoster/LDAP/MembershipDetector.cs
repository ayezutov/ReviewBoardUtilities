using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace ReviewBoardTfsAutoMerger.LDAP
{
    public class MembershipDetector
    {
         public static List<Principal> GetAllUserGroups(Principal user, PrincipalContext context = null, List<Principal> preresult = null)
         {
             if (user == null)
                 return new List<Principal>();

             context = context ?? user.Context;
             preresult = preresult ?? new List<Principal>();
             
             var groups = user.GetGroups(context).ToList();
             
             foreach (var @group in groups)
             {
                 if (preresult.Any(g2 => @group.SamAccountName.Equals(g2.SamAccountName)))
                     continue;

                 preresult.Add(@group);
                 GetAllUserGroups(@group, context, preresult);
             }

             return preresult;
         }
    }
}