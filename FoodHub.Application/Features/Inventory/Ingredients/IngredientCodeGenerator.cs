using System.Globalization;
using System.Text;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Inventory.Ingredients
{
    public static class IngredientCodeGenerator
    {
        private const int _maxIngredientCodeLength = 20;

        public static async Task<string> GenerateAsync(
            IGenericRepository<Ingredient> repo,
            string ingredientName
        )
        {
            var baseCode = Normalize(ingredientName);
            var nextSequence = await repo.CountAsync(_ => true) + 1;
            return AppendSuffix(baseCode, nextSequence);
        }

        public static string Normalize(string ingredientName)
        {
            var normalized = ingredientName.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
                if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            var code = builder.ToString();
            if (string.IsNullOrWhiteSpace(code))
            {
                code = "INGREDIENT";
            }

            return code.Length <= _maxIngredientCodeLength
                ? code
                : code[.._maxIngredientCodeLength];
        }

        private static string AppendSuffix(string baseCode, int suffix)
        {
            var suffixText = $"-{suffix.ToString(CultureInfo.InvariantCulture)}";
            var maxBaseLength = _maxIngredientCodeLength - suffixText.Length;

            if (maxBaseLength <= 0)
            {
                return suffixText[.._maxIngredientCodeLength];
            }

            var truncatedBase = baseCode.Length <= maxBaseLength
                ? baseCode
                : baseCode[..maxBaseLength];

            return truncatedBase + suffixText;
        }
    }
}
