namespace Invoices.Rendering.Receipt;

using Invoices.Rendering.Text;

public class StandardFont : RenderFont
{
	public StandardFont()
		: base("Roboto Mono")
	{

	}

	public override int LineCharacterWidth => 45;
	public override double FontSize => 21.25;

	public static readonly StandardFont SingletonInstance = new StandardFont();
}