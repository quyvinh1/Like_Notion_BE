using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Attributes
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            ErrorMessage = "The date must be in the future.";
        }
        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue > DateTime.Now;
            }

            return false;
        }
    }
}
