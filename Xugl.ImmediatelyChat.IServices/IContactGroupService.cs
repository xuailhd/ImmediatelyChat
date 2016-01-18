using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Model;

namespace Xugl.ImmediatelyChat.IServices
{
    public interface IContactGroupService
    {
        IList<ContactPerson> GetContactPersonIDListByGroupID(string groupID);

    }
}
