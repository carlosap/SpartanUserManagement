using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpartanEnvironment;

namespace SpartanUserManagement.Test
{

    [TestClass]
    public class UserManagementApiTest
    {
        private UserManagementApi _users;
        private User _user;
        private static IEnviroment _env;
        [TestMethod]
        public void TestUserManagementSettings()
        {
            _env = new Environment();
            _env.SetUserVariable("Environment", "Development");
            var _ums = new UserManagementApi();
            var _testConnection = _ums.ConnectionString;
            var _testEnvString = _ums.Environment;
            Assert.IsTrue(_testEnvString.Equals("Development"));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(_testConnection), "No Connection found");

        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestAddNewUserByUserNameAsync()
        {
            _env = new Environment();
            _users = new UserManagementApi();
            _env.SetUserVariable("Environment", "Development");
            _user = new User
            {

                //1- Create Dummy User
                Id = System.Guid.Parse("F037567D-54BC-4044-A6F4-66A7E85A0E34"),
                UserName = "cperez",
                GivenName = "carlos",
                SurName = "perez",
                PasswordHash = "TestGoog!e1",
                Email = "cperez@donotreply.com"
            };

            //2- Delete records
            await _users.DeleteAllUsers();
            await _users.DeleteRoles();


            //3- Add User Records
            var _userResponse = await _users.AddOrUpdateUserWithUserName(_user);
            Assert.IsTrue(_userResponse.Status.Equals("ok"), _userResponse.Msg);

            //4- Update User - changed username and same password
            _user.UserName = "cperez1";
            _user.PasswordHash = "TestGoog!e1";
            _userResponse = await _users.AddOrUpdateUserWithUserName(_user);
            Assert.IsTrue(_userResponse.Status.Equals("ok"), _userResponse.Msg);

            //5- Lock The Account
            _userResponse = await _users.SetLockState(_user.Id, "This account is locked due to payments", true);
            Assert.IsTrue(_userResponse.LockEnabled, _userResponse.Msg);

            System.Threading.Thread.Sleep(500);

            //5- Unlock The Account
            _userResponse = await _users.SetLockState(_user.Id, "Payments Received for $200", false);
            Assert.IsTrue(!_userResponse.LockEnabled, _userResponse.Msg);

            //6- Disable the account and verify
            System.Threading.Thread.Sleep(500);
            _userResponse = await _users.SetActiveState(_user.Id, "Deleting the Account for Temp reasons!", false);
            Assert.IsTrue(!_userResponse.IsActive, "failed to disable the user account");


            //7- Enable the account and verify
            System.Threading.Thread.Sleep(500);
            _userResponse = await _users.SetActiveState(_user.Id, "Found the Problem. Account was enable after receiving the email from Peter", true);
            Assert.IsTrue(_userResponse.IsActive, "failed to enable the user account");

            //8- Find User By UserName
            _userResponse = await _users.GetUserByUserName("cperez1");
            Assert.IsTrue((_userResponse.Status.Equals("ok") && _userResponse.Email.Equals("cperez@donotreply.com")), _userResponse.Msg);

            //9- Find User By Email
            _userResponse = await _users.GetUserByEmail("cperez@donotreply.com");
            Assert.IsTrue((_userResponse.Status.Equals("ok") && _userResponse.UserName.Equals("cperez1")), _userResponse.Msg);

            //10- Delete the User Account 
            _userResponse = await _users.DeleteUserAccount(_user.Id, "Testing Delete Account");
            var _userList = await _users.GetActiveUsers();
            Assert.IsTrue(_userList.Count == 0, "Failed to Delete Account");

            //11- Set the Account Active again
            _userResponse = await _users.SetActiveState(_user.Id, "User is back", true);
            Assert.IsTrue(_userResponse.IsActive, "failed to enable the user account");

            //12- Reset Password- Mismatch Error
            _userResponse = await _users.ResetPassword(_user.Email, "TestGoog!e3", "TestGoog!e2");
            Assert.IsTrue(_userResponse.Status.Equals("error"), _userResponse.Msg);

            //12- Reset Password- Mismatch and change password
            _userResponse = await _users.ResetPassword(_user.Email, "TestGoog!e1", "TestGoog!e2");
            Assert.IsTrue(_userResponse.Status.Equals("ok"), _userResponse.Msg);

            //13- Login with New password
            _userResponse = await _users.LoginByEmail(_user.Email, "TestGoog!e2");
            Assert.IsTrue(_userResponse.Status.Equals("ok"), _userResponse.Msg);

            //14- Login with invalid params
            _userResponse = await _users.LoginByEmail(_user.Email, "");
            Assert.IsTrue(_userResponse.Status.Equals("error"), _userResponse.Msg);

            //15- Login with invalid email
            _userResponse = await _users.LoginByEmail("123@", "TestGoog!e2");
            Assert.IsTrue(_userResponse.Status.Equals("error"), _userResponse.Msg);

            //16- Login with old password
            _userResponse = await _users.LoginByEmail(_user.Email, "TestGoog!e1");
            Assert.IsTrue(_userResponse.Status.Equals("error"), _userResponse.Msg);

            //17- Login with New password...again
            var loginemail = new LoginEmail
            {
                Email = _user.Email,
                Password = "TestGoog!e2"
            };
            _userResponse = await _users.LoginByEmail(loginemail);
            Assert.IsTrue(_userResponse.Status.Equals("ok"), _userResponse.Msg);

            //18- Login by UserName
            var userlogin = new LoginUser
            {
                UserName = _user.UserName,
                Password = "TestGoog!e2"
            };
            _userResponse = await _users.LoginByUserName(userlogin);
            Assert.IsTrue(_userResponse.Status.Equals("ok"), _userResponse.Msg);

            //19- Verify User Counts
            _userList = await _users.GetActiveUsers();
            Assert.IsTrue(_userList.Count > 0, "Failed to Retrived All Users");

            //ROLES::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
            //20- Add Roles
            var _roleName = "anonymous user";
            var _role = await _users.AddRole(_roleName);
            Assert.IsTrue(_role.RoleName.Equals(_roleName), $"unable to add new role: {_roleName}");

            //21- Add Roles
            _roleName = "authenticated user";
            _role = await _users.AddRole(_roleName);
            Assert.IsTrue(_role.RoleName.Equals(_roleName), $"unable to add new role: {_roleName}");

            //22- make sure no duplicate names are added
            _roleName = "authenticated user";
            _role = await _users.AddRole(_roleName);
            Assert.IsTrue(_role.RoleName.Equals(_roleName), $"unable to add new role: {_roleName}");

            //23- Add Roles
            _roleName = "administrator";
            _role = await _users.AddRole(_roleName);
            Assert.IsTrue(_role.RoleName.Equals(_roleName), $"unable to add new role: {_roleName}");
            var updateId = _role.Id;

            //24- Add dummy role to delete
            _roleName = "dummy";
            _role = await _users.AddRole(_roleName);
            Assert.IsTrue(_role.RoleName.Equals(_roleName), $"unable to add new role: {_roleName}");

            _role = await _users.GetRole(_role.Id);
            Assert.IsTrue(_role.IsActive, $"unable to get role: {_role.Id}");


            _roleName = "dummy";
            await _users.DeleteRole(_role.Id);

            _role = await _users.GetRoleByName("dummy");
            Assert.IsTrue(_role == null, $"unable to delete dummy");

            //25- Update Role
            _roleName = "administrator2";
            _role = await _users.UpdateRole(updateId, "administrator2");
            Assert.IsTrue(_role.RoleName.Equals(_roleName));

            //26- Register a New User with email (may consider deleting "user")
            _userResponse = await _users.Register("perezca@donotreplay.com", "JP632tilla1!", "carlos perez");
            Assert.IsTrue(_userResponse.Status.Equals("ok"), _userResponse.Msg);

        }
    }


}
