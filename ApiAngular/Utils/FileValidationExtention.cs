namespace ApiAngular.Utils
{
    public static class FileValidationExtention
    {

        public static bool ValidFile(this IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    throw new ArgumentException(message: "File không được null.");
                }

                // Định nghĩa kích thước tối đa cho ảnh và video
                const long maxImageSize = 5 * 1024 * 1024; // 5MB cho ảnh
                const long maxVideoSize = 15 * 1024 * 1024; // 15MB cho video

                // Lấy loại file (đuôi file)
                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                // Kiểm tra nếu file là ảnh
                if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
                {
                    if (file.Length > maxImageSize)
                    {
                        throw new ArgumentException(message: "Kích thước file ảnh phải nhỏ hơn 5MB.");
                    }
                }
                // Kiểm tra nếu file là video
                else if (fileExtension == ".mp4" || fileExtension == ".mov" || fileExtension == ".avi")
                {
                    if (file.Length > maxVideoSize)
                    {
                        throw new ArgumentException(message: "Kích thước file video phải nhỏ hơn 15MB.");
                    }
                }
                else
                {
                    throw new ArgumentException(message: "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png) hoặc file video (.mp4, .mov, .avi).");
                }

                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
