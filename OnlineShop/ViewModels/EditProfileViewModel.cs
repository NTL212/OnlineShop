using OnlineShop.Models;
using System.Collections.Generic;
using System;

namespace OnlineShop.ViewModels
{
    public class EditProfileViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
    }
}
