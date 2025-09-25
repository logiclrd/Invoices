using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering;

using Invoices.Core;
using Invoices.Rendering.Plan;

public abstract class InvoiceRenderer
{
	public abstract BitmapSource RenderImage(Invoice invoice);
	public abstract RenderPlan CreatePlan(Invoice invoice);
	public abstract UIElement ConstructVisual(RenderPlan plan, int pixelWidth);
}