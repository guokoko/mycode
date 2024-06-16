namespace CTO.Price.Admin.Data
{
    public class UserRoleViewModel
    {
        public UserRoleViewModel()
        {
            User = new ApplicationUser();
            UserRole = new UserRole();
        }
        public ApplicationUser User { get; set; }
        
        public UserRole UserRole { get; set; }
    }
}