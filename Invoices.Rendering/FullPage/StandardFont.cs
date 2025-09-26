namespace Invoices.Rendering.FullPage;

using Invoices.Rendering.Text;

public class StandardFont : RenderFont
{
	StandardFont()
		: base("Poppins")
	{
	}

	public static readonly StandardFont SingletonInstance = new StandardFont();

	public override int LineCharacterWidth => -1; /* not fixed-width */
	public override double FontSize => 16;
}