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

        ContactPerson FindContactPerson(string objectID);
        int InsertNewPerson(ContactPerson contactPerson);
        int UpdatePerson(ContactPerson contactPerson);

        ContactPersonList FindContactPersonList(string objectID, string destinationObjectID);
        int InsertContactPersonList(ContactPersonList contactPersonList);
        int UpdateContactPersonList(ContactPersonList contactPersonList);

        ContactGroup FindContactGroup(string groupID);
        int InsertNewGroup(ContactGroup contactGroup);
        int UpdateGroup(ContactGroup contactGroup);



        int InsertDefaultGroup(string objectID);

        IList<ContactGroupSub> GetLastestContactGroupSub(string objectID, DateTime updateTime);

        IList<ContactPersonList> GetLastestContactPersonList(string objectID, DateTime updateTime);

        IList<ContactGroup> GetLastestContactGroup(string objectID, DateTime updateTime);
    }
}
