using ElmSharp;
using EToolbar = ElmSharp.Toolbar;

namespace Microsoft.Maui.Controls.Compatibility.Platform.Tizen
{
	public class ShellTabs : EToolbar, IShellTabs
	{

		ShellTabsType _type;
		public ShellTabs(EvasObject parent) : base(parent)
		{
			Style = ThemeConstants.Toolbar.Styles.Material;
			SelectionMode = ToolbarSelectionMode.Always;
		}

		public ShellTabsType Scrollable
		{
			get => _type;
			set
			{
				switch (value)
				{
					case ShellTabsType.Fixed:
						this.ShrinkMode = ToolbarShrinkMode.Expand;
						break;
					case ShellTabsType.Scrollable:
						this.ShrinkMode = ToolbarShrinkMode.Scroll;
						break;
				}
				_type = value;
			}
		}

		public EvasObject NativeView
		{
			get
			{
				return this;
			}
		}
	}
}
