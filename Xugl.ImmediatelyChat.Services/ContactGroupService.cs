using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.Services
{
    public class ContactGroupService : IContactGroupService
    {
        private readonly IRepository<ContactPerson> contactPersonRepository;
        private readonly IRepository<ContactGroupSub> contactGroupSubRepository;

        public ContactGroupService(IRepository<ContactPerson> contactPersonRepository, IRepository<ContactGroupSub> contactGroupSubRepository)
        {
            this.contactPersonRepository = contactPersonRepository;
            this.contactGroupSubRepository = contactGroupSubRepository;
        }

        public IList<ContactPerson> GetContactPersonIDListByGroupID(string groupID)
        {

            var contactPersons = from contactGroup in contactGroupSubRepository.Table
                                 join contactPerson in contactPersonRepository.Table on contactGroup.ContactPersonObjectID equals contactPerson.ObjectID
                                 where contactGroup.ContactGroupID == groupID
                                 select contactPerson;
            return contactPersons.ToList();
        }
    }
}
