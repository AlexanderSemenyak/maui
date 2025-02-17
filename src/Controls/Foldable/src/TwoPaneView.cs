using System;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui;
using System.Threading;
using Microsoft.Maui.Foldable;

namespace Microsoft.Maui.Controls.Foldable
{
	[ContentProperty("")]
	public partial class TwoPaneView : Grid
	{
		enum ViewMode
		{
			Pane1Only,
			Pane2Only,
			LeftRight,
			RightLeft,
			TopBottom,
			BottomTop,
			None
		};

		TwoPaneViewLayoutGuide _twoPaneViewLayoutGuide;
		ContentView _content1;
		ContentView _content2;
		ViewMode _currentMode;
		bool _updatingMode = false;
		bool _processPendingChange = false;
		Rect _layoutGuidePane1;
		Rect _layoutGuidePane2;
		TwoPaneViewMode _layoutGuideMode;
		Rect _layoutGuideHinge;
		bool _layoutGuideIsLandscape;
		double _previousWidth = -1;
		double _previousHeight = -1;

		public static readonly BindableProperty TallModeConfigurationProperty
			= BindableProperty.Create("TallModeConfiguration", typeof(TwoPaneViewTallModeConfiguration), typeof(TwoPaneView), defaultValue: TwoPaneViewTallModeConfiguration.TopBottom, propertyChanged: TwoPaneViewLayoutPropertyChanged);

		public static readonly BindableProperty WideModeConfigurationProperty
			= BindableProperty.Create("WideModeConfiguration", typeof(TwoPaneViewWideModeConfiguration), typeof(TwoPaneView), defaultValue: TwoPaneViewWideModeConfiguration.LeftRight, propertyChanged: TwoPaneViewLayoutPropertyChanged);

		public static readonly BindableProperty Pane1Property
			= BindableProperty.Create("Pane1", typeof(View), typeof(TwoPaneView), propertyChanged: (b, o, n) => OnPanePropertyChanged(b, o, n, 0));

		public static readonly BindableProperty Pane2Property
			= BindableProperty.Create("Pane2", typeof(View), typeof(TwoPaneView), propertyChanged: (b, o, n) => OnPanePropertyChanged(b, o, n, 1));

		static readonly BindablePropertyKey ModePropertyKey
			= BindableProperty.CreateReadOnly("Mode", typeof(TwoPaneViewMode), typeof(TwoPaneView), defaultValue: TwoPaneViewMode.SinglePane, propertyChanged: OnModePropertyChanged);

		public static readonly BindableProperty ModeProperty = ModePropertyKey.BindableProperty;

		public static readonly BindableProperty PanePriorityProperty
			= BindableProperty.Create("PanePriority", typeof(TwoPaneViewPriority), typeof(TwoPaneView), defaultValue: TwoPaneViewPriority.Pane1, propertyChanged: TwoPaneViewLayoutPropertyChanged);

		public static readonly BindableProperty MinTallModeHeightProperty
			= BindableProperty.Create("MinTallModeHeight", typeof(double), typeof(TwoPaneView), defaultValueCreator: OnMinModePropertyCreate, propertyChanged: TwoPaneViewLayoutPropertyChanged);

		public static readonly BindableProperty MinWideModeWidthProperty
			= BindableProperty.Create("MinWideModeWidth", typeof(double), typeof(TwoPaneView), defaultValueCreator: OnMinModePropertyCreate, propertyChanged: TwoPaneViewLayoutPropertyChanged);

		public static readonly BindableProperty Pane1LengthProperty
			= BindableProperty.Create("Pane1Length", typeof(GridLength), typeof(TwoPaneView), defaultValue: GridLength.Star, propertyChanged: TwoPaneViewLayoutPropertyChanged);

		public static readonly BindableProperty Pane2LengthProperty
			= BindableProperty.Create("Pane2Length", typeof(GridLength), typeof(TwoPaneView), defaultValue: GridLength.Star, propertyChanged: TwoPaneViewLayoutPropertyChanged);

		public event EventHandler ModeChanged;

		static object OnMinModePropertyCreate(BindableObject bindable)
		{
			double returnValue = 641d;
			if (DeviceDisplay.MainDisplayInfo.Density <= 0)
				return returnValue;

			returnValue = 641d / DeviceDisplay.MainDisplayInfo.Density;
			return returnValue;
		}


		static void OnModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((TwoPaneView)bindable).ModeChanged?.Invoke(bindable, EventArgs.Empty);
		}


		static void TwoPaneViewLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var b = (TwoPaneView)bindable;
			b.UpdateMode();
		}

		static void OnPanePropertyChanged(BindableObject bindable, object oldValue, object newValue, int paneIndex)
		{
			TwoPaneView twoPaneView = (TwoPaneView)bindable;
			var newView = (View)newValue;

			if (paneIndex == 0)
				twoPaneView._content1.Content = newView;
			else
				twoPaneView._content2.Content = newView;

			twoPaneView.UpdateMode();
		}

		public double MinTallModeHeight
		{
			get { return (double)GetValue(MinTallModeHeightProperty); }
			set { SetValue(MinTallModeHeightProperty, value); }
		}
		public double MinWideModeWidth
		{
			get { return (double)GetValue(MinWideModeWidthProperty); }
			set { SetValue(MinWideModeWidthProperty, value); }
		}

		public GridLength Pane1Length
		{
			get { return (GridLength)GetValue(Pane1LengthProperty); }
			set { SetValue(Pane1LengthProperty, value); }
		}
		public GridLength Pane2Length
		{
			get { return (GridLength)GetValue(Pane2LengthProperty); }
			set { SetValue(Pane2LengthProperty, value); }
		}

		public TwoPaneViewMode Mode { get => (TwoPaneViewMode)GetValue(ModeProperty); }

		public TwoPaneViewTallModeConfiguration TallModeConfiguration
		{
			get { return (TwoPaneViewTallModeConfiguration)GetValue(TallModeConfigurationProperty); }
			set { SetValue(TallModeConfigurationProperty, value); }
		}

		public TwoPaneViewWideModeConfiguration WideModeConfiguration
		{
			get { return (TwoPaneViewWideModeConfiguration)GetValue(WideModeConfigurationProperty); }
			set { SetValue(WideModeConfigurationProperty, value); }
		}

		public View Pane1
		{
			get { return (View)GetValue(Pane1Property); }
			set { SetValue(Pane1Property, value); }
		}

		public View Pane2
		{
			get { return (View)GetValue(Pane2Property); }
			set { SetValue(Pane2Property, value); }
		}

		public TwoPaneViewPriority PanePriority
		{
			get { return (TwoPaneViewPriority)GetValue(PanePriorityProperty); }
			set { SetValue(PanePriorityProperty, value); }
		}

		public TwoPaneView() : this(null)
		{
		}

		internal TwoPaneView(IFoldableService dualScreenService)
		{
			_twoPaneViewLayoutGuide = new TwoPaneViewLayoutGuide(this, dualScreenService);
			_content1 = new ContentView();
			_content2 = new ContentView();

			Children.Add(_content1);
			Children.Add(_content2);

			this.VerticalOptions = LayoutOptions.Fill;
			this.HorizontalOptions = LayoutOptions.Fill;
			ColumnSpacing = 0;
			RowSpacing = 0;

			RowDefinitions = new RowDefinitionCollection() { new RowDefinition(), new RowDefinition(), new RowDefinition() };
			ColumnDefinitions = new ColumnDefinitionCollection() { new ColumnDefinition(), new ColumnDefinition(), new ColumnDefinition() };

		}

		private protected override void OnHandlerChangingCore(HandlerChangingEventArgs args)
		{
			base.OnHandlerChangingCore(args);

			if (_twoPaneViewLayoutGuide.DualScreenService != null)
			{
				_twoPaneViewLayoutGuide.DualScreenService.OnLayoutChanged += DualScreenService_OnFeatureChanged;
			}
		}

		private void DualScreenService_OnFeatureChanged(object sender, FoldEventArgs e)
		{
			System.Diagnostics.Debug.Write("TwoPaneView.DualScreenService_OnFeatureChanged - " + e, "JWM");
			try
			{
				InvalidateMeasure();
			}
			catch (Exception)
			{

			}
		}

		internal override void OnIsPlatformEnabledChanged()
		{
			base.OnIsPlatformEnabledChanged();
			if (IsPlatformEnabled)
			{
				_twoPaneViewLayoutGuide.PropertyChanged += OnTwoPaneViewLayoutGuide;
			}
			else
			{
				_twoPaneViewLayoutGuide.PropertyChanged -= OnTwoPaneViewLayoutGuide;
			}
		}

		void OnTwoPaneViewLayoutGuide(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (_twoPaneViewLayoutGuide.Pane1 == _layoutGuidePane1 &&
				_twoPaneViewLayoutGuide.Pane2 == _layoutGuidePane2 &&
				_twoPaneViewLayoutGuide.Mode == _layoutGuideMode &&
				_twoPaneViewLayoutGuide.Hinge == _layoutGuideHinge &&
				_twoPaneViewLayoutGuide.IsLandscape == _layoutGuideIsLandscape)
			{
				return;
			}

			_layoutGuidePane1 = _twoPaneViewLayoutGuide.Pane1;
			_layoutGuidePane2 = _twoPaneViewLayoutGuide.Pane2;
			_layoutGuideMode = _twoPaneViewLayoutGuide.Mode;
			_layoutGuideHinge = _twoPaneViewLayoutGuide.Hinge;
			_layoutGuideIsLandscape = _twoPaneViewLayoutGuide.IsLandscape;

			InvalidateMeasure();
		}

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			UpdateMode(widthConstraint, heightConstraint, false);
			var sizeRequest = base.MeasureOverride(widthConstraint, heightConstraint);
			return sizeRequest;
		}

		protected override Size ArrangeOverride(Rect bounds)
		{
			return base.ArrangeOverride(bounds);
		}

		void UpdateMode(bool invalidateLayout = true)
		{
			UpdateMode(Width, Height, invalidateLayout);
		}

		bool UpdateMode(double width, double height, bool invalidateLayout = true)
		{
			invalidateLayout = false;

			// controls hasn't fully been created yet
			if (RowDefinitions.Count != 3
				|| ColumnDefinitions.Count != 3
				|| width == -1
				|| height == -1
				|| width == double.PositiveInfinity
				|| height == double.PositiveInfinity)
			{
				return false;
			}


			if (_updatingMode)
			{
				_processPendingChange = true;
				return false;
			}

			_updatingMode = true;
			try
			{
				double controlWidth = width;
				double controlHeight = height;
				_previousWidth = width;
				_previousHeight = height;

				ViewMode newMode = (PanePriority == TwoPaneViewPriority.Pane1) ? ViewMode.Pane1Only : ViewMode.Pane2Only;

				_twoPaneViewLayoutGuide.UpdateLayouts(width, height);

				if (_twoPaneViewLayoutGuide.Mode != TwoPaneViewMode.SinglePane)
				{
					if (_twoPaneViewLayoutGuide.Mode == TwoPaneViewMode.Wide)
					{
						// Regions are laid out horizontally
						if (WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane)
						{
							newMode = (WideModeConfiguration == TwoPaneViewWideModeConfiguration.LeftRight) ? ViewMode.LeftRight : ViewMode.RightLeft;
						}
					}
					else if (_twoPaneViewLayoutGuide.Mode == TwoPaneViewMode.Tall)
					{
						// Regions are laid out vertically
						if (TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane)
						{
							newMode = (TallModeConfiguration == TwoPaneViewTallModeConfiguration.TopBottom) ? ViewMode.TopBottom : ViewMode.BottomTop;
						}
					}
				}
				else
				{
					// One region
					if (controlWidth > MinWideModeWidth && WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane)
					{
						// Split horizontally
						newMode = (WideModeConfiguration == TwoPaneViewWideModeConfiguration.LeftRight) ? ViewMode.LeftRight : ViewMode.RightLeft;
					}
					else if (controlHeight > MinTallModeHeight && TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane)
					{
						// Split vertically
						newMode = (TallModeConfiguration == TwoPaneViewTallModeConfiguration.TopBottom) ? ViewMode.TopBottom : ViewMode.BottomTop;
					}
				}

				// Update row/column sizes (this may need to happen even if the mode doesn't change)
				UpdateRowsColumns(newMode, width, height);

				// Update mode if necessary
				if (newMode != _currentMode)
				{
					_currentMode = newMode;

					TwoPaneViewMode newViewMode = TwoPaneViewMode.SinglePane;

					switch (_currentMode)
					{
						case ViewMode.Pane1Only:
							break;
						case ViewMode.Pane2Only:
							break;
						case ViewMode.LeftRight:
							newViewMode = TwoPaneViewMode.Wide;
							break;
						case ViewMode.RightLeft:
							newViewMode = TwoPaneViewMode.Wide;
							break;
						case ViewMode.TopBottom:
							newViewMode = TwoPaneViewMode.Tall;
							break;
						case ViewMode.BottomTop:
							newViewMode = TwoPaneViewMode.Tall;
							break;
					}

					if (newViewMode != Mode)
					{
						_updatingMode = false;
						SetValue(ModePropertyKey, newViewMode);
					}
				}

				_updatingMode = false;

				if (_processPendingChange)
				{
					_processPendingChange = false;
					UpdateMode();
				}
				else
				{
					if (invalidateLayout)
						InvalidateMeasure(); //HACK:FOLDABLE was InvalidateLayout();
				}
			}
			finally
			{
				_updatingMode = false;
			}

			return true;
		}

		Rect _previousHinge = Rect.Zero;

		void UpdateRowsColumns(ViewMode newMode, double newWidth, double newHeight)
		{
			var columnLeft = ColumnDefinitions[0];
			var columnMiddle = ColumnDefinitions[1];
			var columnRight = ColumnDefinitions[2];

			var rowTop = RowDefinitions[0];
			var rowMiddle = RowDefinitions[1];
			var rowBottom = RowDefinitions[2];

			Rect pane1 = _twoPaneViewLayoutGuide.Pane1;
			Rect pane2 = _twoPaneViewLayoutGuide.Pane2;
			bool isLayoutSpanned = _twoPaneViewLayoutGuide.Mode != TwoPaneViewMode.SinglePane;

			_previousHinge = _twoPaneViewLayoutGuide.Hinge;

			if (_twoPaneViewLayoutGuide.Mode != TwoPaneViewMode.SinglePane && newMode != ViewMode.Pane1Only && newMode != ViewMode.Pane2Only)
			{
				_previousHinge = _twoPaneViewLayoutGuide.Hinge;
				Rect hinge = _twoPaneViewLayoutGuide.Hinge;

				if (_twoPaneViewLayoutGuide.Mode == TwoPaneViewMode.Wide)
				{
					columnMiddle.Width = new GridLength(hinge.Width, GridUnitType.Absolute);
					columnLeft.Width = new GridLength(pane1.Width, GridUnitType.Absolute);
					columnRight.Width = new GridLength(pane2.Width, GridUnitType.Absolute);

					rowMiddle.Height = new GridLength(0, GridUnitType.Absolute);
					rowTop.Height = GridLength.Star;
					rowBottom.Height = new GridLength(0, GridUnitType.Absolute);
				}
				else
				{
					rowMiddle.Height = new GridLength(hinge.Height, GridUnitType.Absolute);
					rowTop.Height = new GridLength(pane1.Height, GridUnitType.Absolute);
					rowBottom.Height = new GridLength(pane2.Height, GridUnitType.Absolute);

					columnMiddle.Width = new GridLength(0, GridUnitType.Absolute);
					columnLeft.Width = GridLength.Star;
					columnRight.Width = new GridLength(0, GridUnitType.Absolute);
				}
			}
			else
			{
				columnMiddle.Width = new GridLength(0, GridUnitType.Absolute);
				rowMiddle.Height = new GridLength(0, GridUnitType.Absolute);

				if (newMode == ViewMode.LeftRight || newMode == ViewMode.RightLeft)
				{
					columnLeft.Width = ((newMode == ViewMode.LeftRight) ? Pane1Length : Pane2Length);
					columnRight.Width = ((newMode == ViewMode.LeftRight) ? Pane2Length : Pane1Length);
				}
				else
				{
					columnLeft.Width = new GridLength(1, GridUnitType.Star);
					columnRight.Width = new GridLength(0, GridUnitType.Absolute);
				}

				if (newMode == ViewMode.TopBottom || newMode == ViewMode.BottomTop)
				{
					rowTop.Height = ((newMode == ViewMode.TopBottom) ? Pane1Length : Pane2Length);
					rowBottom.Height = ((newMode == ViewMode.TopBottom) ? Pane2Length : Pane1Length);
				}
				else
				{
					rowTop.Height = new GridLength(1, GridUnitType.Star);
					rowBottom.Height = new GridLength(0, GridUnitType.Absolute);
				}
			}

			switch (newMode)
			{
				case ViewMode.LeftRight:
					SetRowColumn(_content1, 0, 0);
					SetRowColumn(_content2, 0, 2);
					_content1.IsVisible = true;
					_content2.IsVisible = true;

					if (!isLayoutSpanned)
					{
						// add padding to content where the content is under the hinge
						_content1.Padding = new Thickness(pane1.X, 0, 0, 0);
						_content2.Padding = new Thickness(0, 0, newWidth - pane1.Width, 0);
					}
					else
					{
						_content1.Padding = new Thickness(0, 0, 0, 0);
						_content2.Padding = new Thickness(0, 0, 0, 0);
					}
					break;
				case ViewMode.RightLeft:
					SetRowColumn(_content1, 0, 2);
					SetRowColumn(_content2, 0, 0);
					_content1.IsVisible = true;
					_content2.IsVisible = true;

					if (!isLayoutSpanned)
					{
						// add padding to content where the content is under the hinge
						_content2.Padding = new Thickness(pane1.X, 0, 0, 0);
						_content1.Padding = new Thickness(0, 0, newWidth - pane1.Width, 0);
					}
					else
					{
						_content1.Padding = new Thickness(0, 0, 0, 0);
						_content2.Padding = new Thickness(0, 0, 0, 0);
					}
					break;
				case ViewMode.TopBottom:
					SetRowColumn(_content1, 0, 0);
					SetRowColumn(_content2, 2, 0);
					_content1.IsVisible = true;
					_content2.IsVisible = true;

					if (!isLayoutSpanned)
					{
						// add padding to content where the content is under the hinge
						_content1.Padding = new Thickness(0, pane1.Y, 0, 0);
						_content2.Padding = new Thickness(0, 0, 0, newHeight - pane1.Height);
					}
					else
					{
						_content1.Padding = new Thickness(0, 0, 0, 0);
						_content2.Padding = new Thickness(0, 0, 0, 0);
					}

					break;
				case ViewMode.BottomTop:
					SetRowColumn(_content1, 2, 0);
					SetRowColumn(_content2, 0, 0);
					_content1.IsVisible = true;
					_content2.IsVisible = true;

					if (!isLayoutSpanned)
					{
						// add padding to content where the content is under the hinge
						_content2.Padding = new Thickness(0, pane1.Y, 0, 0);
						_content1.Padding = new Thickness(0, 0, 0, newHeight - pane1.Height);
					}
					else
					{
						_content1.Padding = new Thickness(0, 0, 0, 0);
						_content2.Padding = new Thickness(0, 0, 0, 0);
					}
					break;
				case ViewMode.Pane1Only:
					SetRowColumn(_content1, 0, 0);
					SetRowColumn(_content2, 0, 2);
					_content1.IsVisible = true;
					_content2.IsVisible = false;
					if (!isLayoutSpanned)
					{
						// add padding to content where the content is under the hinge
						_content1.Padding = new Thickness(pane1.X, pane1.Y, newWidth - pane1.Width - pane1.X, newHeight - pane1.Height - pane1.Y);
						_content2.Padding = new Thickness(0, 0, 0, 0);
					}
					else
					{
						_content1.Padding = new Thickness(0, 0, 0, 0);
						_content2.Padding = new Thickness(0, 0, 0, 0);
					}
					break;
				case ViewMode.Pane2Only:
					SetRowColumn(_content1, 0, 2);
					SetRowColumn(_content2, 0, 0);
					_content1.IsVisible = false;
					_content2.IsVisible = true;
					if (!isLayoutSpanned)
					{
						_content1.Padding = new Thickness(0, 0, 0, 0);
						// add padding to content where the content is under the hinge
						_content2.Padding = new Thickness(pane1.X, pane1.Y, newWidth - pane1.Width - pane1.X, newHeight - pane1.Height - pane1.Y);
					}
					else
					{
						_content1.Padding = new Thickness(0, 0, 0, 0);
						_content2.Padding = new Thickness(0, 0, 0, 0);
					}
					break;
			}

			void SetRowColumn(BindableObject bo, int row, int column)
			{
				if (bo == null)
					return;

				Grid.SetColumn(bo, column);
				Grid.SetRow(bo, row);
			}
		}
	}
}
