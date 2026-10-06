using Microsoft.AspNetCore.Authorization;

//This class represents the authorization rule itself.
//it does Not contain logic.
//it simply defined the requirement:
//Owner OR Admin can access the student resource;

namespace SmartAcademyApi.Authorization
{
    public class StudentOwnerOrAdminRequirement: IAuthorizationRequirement
    {
    }
}
