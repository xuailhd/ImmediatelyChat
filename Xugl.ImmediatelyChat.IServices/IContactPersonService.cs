using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Model;

namespace Xugl.ImmediatelyChat.IServices
{
    public interface IContactPersonService
    {
        IList<ContactPerson> GetContactPersonIDListByGroupID(string groupID);

        int InsertNewPerson(ContactPerson contactPerson);

        int InsertDefaultGroup(string ObjectID);
    }
}
