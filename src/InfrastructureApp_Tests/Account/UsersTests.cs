using InfrastructureApp.Models;

namespace InfrastructureApp_Tests;

[TestFixture]
public class UsersTests
{
    private Users usersModel;

    [Test]
    public void Users_ChecksUserName_UserNameIsNormalized()
    {
        string userName = "testUser";
        string email = "testUser@test.com";

        usersModel = new Users(userName, email)
        {
            UserName = userName,
            Email = email
        };

        Assert.That(usersModel.NormalizedUserName, Is.EqualTo(userName.ToUpper()));
        Assert.That(usersModel.UserName, Is.EqualTo(userName));
    }
    
    [Test]
    public void Users_ChecksEmail_EmailIsNormalized()
    {
        string userName = "testUser";
        string email = "testUser@test.com";
        
        usersModel = new Users(userName, email)
        {
            UserName = userName,
            Email = email
        };    
        
        Assert.That(usersModel.NormalizedEmail, Is.EqualTo(email.ToUpper()));
        Assert.That(usersModel.Email, Is.EqualTo(email));
    } 
}