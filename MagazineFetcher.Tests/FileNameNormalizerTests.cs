using MagazineFetcher.Classification;

namespace MagazineFetcher.Tests
{
	public class FileNameNormalizerTests
	{
		[Theory]
		[InlineData("ThÇ?ringer Allgemeine.pdf", "Thuringer Allgemeine.pdf")]
		[InlineData("Lƒ??HumanitÇ?.pdf", "L’Humanite.pdf")]
		[InlineData("OberÇ?sterreichische Nachrichten.pdf", "Oberosterreichische Nachrichten.pdf")]
		public void NormalizeFileName_FixesBrokenEncoding(string input, string expected)
		{
			var result = FileRenamer.NormalizeFileName(input);
			Assert.Equal(expected, result);
		}
	}
}
