using System.Text.RegularExpressions;

namespace CEA_RPL.Helpers.Security
{
    public static class MaskingHelper
    {
        /// <summary>
        /// Masks an Aadhaar number. Format: XXXXXXXX1234
        /// </summary>
        public static string MaskAadhaar(string aadhaarNumber)
        {
            if (string.IsNullOrWhiteSpace(aadhaarNumber)) return aadhaarNumber;

            // Remove spaces/hyphens
            string cleanAadhaar = Regex.Replace(aadhaarNumber, @"[\s-]", "");
            
            if (cleanAadhaar.Length != 12) return aadhaarNumber; // Return original if not 12 digits

            // Mask format: XXXXXXXX1234
            string lastFour = cleanAadhaar.Substring(cleanAadhaar.Length - 4);
            return new string('X', 8) + lastFour;
        }

        /// <summary>
        /// Masks a PAN card number. Format: XXXXX1234X
        /// </summary>
        public static string MaskPAN(string panNumber)
        {
            if (string.IsNullOrWhiteSpace(panNumber) || panNumber.Length != 10) return panNumber;

            // Mask format: XXXXX1234X
            string firstPart = new string('X', 5);
            string middlePart = panNumber.Substring(5, 4);
            string lastChar = new string('X', 1);

            return firstPart + middlePart + lastChar;
        }

        /// <summary>
        /// Masks a Mobile Number. Format: XXXXXX1234
        /// </summary>
        public static string MaskMobile(string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber)) return mobileNumber;

            string cleanMobile = Regex.Replace(mobileNumber, @"[^\d]", "");

            if (cleanMobile.Length < 10) return mobileNumber;

            // Mask format: XXXXXX1234
            string lastFour = cleanMobile.Substring(cleanMobile.Length - 4);
            return new string('X', cleanMobile.Length - 4) + lastFour;
        }
    }
}
