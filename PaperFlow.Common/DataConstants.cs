namespace PaperFlow.Common
{
    public class DataConstants
    {
        public static class Paper
        {
            public const int TitleMinLength = 5;
            public const int TitleMaxLength = 500;

            public const int AbstractMaxLength = 10000;
            public const int DoiMaxLength = 255;
            public const int ExternalIdMaxLength = 255;
            public const int UrlMaxLength = 2048;
            public const int FilePathMaxLength = 1024;
            public const int NotesMaxLength = 10000;

            public const int MinRating = 1;
            public const int MaxRating = 5;

            public const int MinPages = 1;
            public const int MaxPages = 100000;
        }

        public static class Author
        {
            public const int NameMaxLength = 200;
            public const int OrcidMaxLength = 19;
        }

        public static class Category
        {
            public const int NameMaxLength = 100;
            public const int DescriptionMaxLength = 500;
        }

        public static class ResearchSource
        {
            public const int NameMaxLength = 100;
            public const int UrlMaxLength = 2048;
        }

    }
}
