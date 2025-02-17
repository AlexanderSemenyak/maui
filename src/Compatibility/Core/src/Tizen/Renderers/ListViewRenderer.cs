using System;
using System.Collections.Specialized;
using ElmSharp;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Devices;
using EColor = ElmSharp.Color;

namespace Microsoft.Maui.Controls.Compatibility.Platform.Tizen
{
	[System.Obsolete(Compatibility.Hosting.MauiAppBuilderExtensions.UseMapperInstead)]
	/// <summary>
	/// Renderer class for Xamarin ListView class. This provides necessary logic translating
	/// Xamarin API to Tizen Native API. This is a derivate of a ViewRenderer base class.
	/// This is a template class with two template parameters. First one is restricted to
	/// Microsoft.Maui.Controls.Compatibility.View and can be accessed via property Element. This represent actual
	/// xamarin view which represents control logic. Second one is restricted to ElmSharp.Widget
	/// types, and can be accessed with Control property. This represents actual native control
	/// which is used to draw control and realize xamarin forms api.
	/// </summary>
	public class ListViewRenderer : ViewRenderer<ListView, Native.ListView>
	{
		IListViewController Controller => Element;
		ITemplatedItemsView<Cell> TemplatedItemsView => Element;

		/// <summary>
		/// The _lastSelectedItem and _selectedItemChanging are used for realizing ItemTapped event. Since Xamarin
		/// needs information only when an item has been taped, native handlers need to be agreagated
		/// and NotifyRowTapped has to be realized with this.
		/// </summary>

		GenListItem _lastSelectedItem = null;
		int _selectedItemChanging = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="Microsoft.Maui.Controls.Compatibility.Platform.Tizen.ListViewRenderer"/> class.
		/// Note that at this stage of construction renderer dose not have required native element. This should
		/// only be used with xamarin engine.
		/// </summary>
		public ListViewRenderer()
		{
			RegisterPropertyHandler(ListView.IsGroupingEnabledProperty, UpdateIsGroupingEnabled);
			RegisterPropertyHandler(ListView.HasUnevenRowsProperty, UpdateHasUnevenRows);
			RegisterPropertyHandler(ListView.RowHeightProperty, UpdateRowHeight);
			RegisterPropertyHandler(ListView.SelectedItemProperty, UpdateSelectedItem);
			RegisterPropertyHandler(ListView.ItemsSourceProperty, UpdateSource);
			RegisterPropertyHandler(nameof(Element.HeaderElement), UpdateHeader);
			RegisterPropertyHandler(nameof(Element.FooterElement), UpdateFooter);
			RegisterPropertyHandler(ListView.SelectionModeProperty, UpdateSelectionMode);
			RegisterPropertyHandler(ListView.VerticalScrollBarVisibilityProperty, UpdateVerticalScrollBarVisibility);
			RegisterPropertyHandler(ListView.HorizontalScrollBarVisibilityProperty, UpdateHorizontalScrollBarVisibility);
			RegisterPropertyHandler(ListView.SeparatorColorProperty, UpdateSeparator);
			RegisterPropertyHandler(ListView.SeparatorVisibilityProperty, UpdateSeparator);
		}

		/// <summary>
		/// Invoked on creation of new ListView renderer. Handles the creation of a native
		/// element and initialization of the renderer.
		/// </summary>
		/// <param name="e"><see cref="Microsoft.Maui.Controls.Compatibility.Platform.Tizen.ElementChangedEventArgs"/>.</param>
		protected override void OnElementChanged(ElementChangedEventArgs<ListView> e)
		{
			if (Control == null)
			{
				SetNativeControl(CreateNativeControl());

				Control.Scrolled += OnScrolled;
				Control.ItemSelected += OnListViewItemSelected;
				Control.ItemUnselected += OnListViewItemUnselected;
			}

			if (e.OldElement != null)
			{
				e.OldElement.ScrollToRequested -= OnScrollToRequested;
				e.OldElement.TemplatedItems.GroupedCollectionChanged -= OnGroupedCollectionChanged;
				e.OldElement.TemplatedItems.CollectionChanged -= OnCollectionChanged;
			}

			if (e.NewElement != null)
			{
				Element.ScrollToRequested += OnScrollToRequested;
				Element.TemplatedItems.GroupedCollectionChanged += OnGroupedCollectionChanged;
				Element.TemplatedItems.CollectionChanged += OnCollectionChanged;
			}

			base.OnElementChanged(e);
		}

		protected virtual Native.ListView CreateNativeControl()
		{
			if (DeviceInfo.Idiom == DeviceIdiom.Watch)
			{
				return new Native.Watch.WatchListView(Forms.NativeParent, Forms.CircleSurface);
			}
			else
			{
				return new Native.ListView(Forms.NativeParent);
			}
		}

		/// <summary>
		/// Handles the disposing of an existing renderer instance. Results in event handlers
		/// being detached and a Dispose() method from base class (VisualElementRenderer) being invoked.
		/// </summary>
		/// <param name="disposing">A boolean flag passed to the invocation of base class' Dispose() method.
		///  <c>True</c> if the memory release was requested on demand.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Element != null)
				{
					Element.ScrollToRequested -= OnScrollToRequested;
					Element.TemplatedItems.CollectionChanged -= OnCollectionChanged;
					Element.TemplatedItems.GroupedCollectionChanged -= OnGroupedCollectionChanged;
				}

				if (Control != null)
				{
					Control.Scrolled -= OnScrolled;
					Control.ItemSelected -= OnListViewItemSelected;
					Control.ItemUnselected -= OnListViewItemUnselected;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Handles item selected event. Note that it has to handle selection also for grouping mode as well.
		/// As a result of this method, ItemTapped event should be invoked in Xamarin.
		/// </summary>
		/// <param name="sender">A native list instance from which the event has originated.</param>
		/// <param name="e">Argument associated with handler, it holds native item for which event was raised</param>
		protected void OnListViewItemSelected(object sender, GenListItemEventArgs e)
		{
			GenListItem item = e.Item;

			_lastSelectedItem = item;

			if (_selectedItemChanging == 0)
			{
				if (item != null)
				{
					int index = -1;
					if (Element.IsGroupingEnabled)
					{
						Native.ListView.ItemContext itemContext = item.Data as Native.ListView.ItemContext;
						if (itemContext.IsGroupItem)
						{
							return;
						}
						else
						{
							int groupIndex = (Element.TemplatedItems as System.Collections.IList).IndexOf(itemContext.ListOfSubItems);
							int inGroupIndex = itemContext.ListOfSubItems.IndexOf(itemContext.Cell);

							++_selectedItemChanging;
							Element.NotifyRowTapped(groupIndex, inGroupIndex);
							--_selectedItemChanging;
						}
					}
					else
					{
						index = Element.TemplatedItems.IndexOf((item.Data as Native.ListView.ItemContext).Cell);

						++_selectedItemChanging;
						Element.NotifyRowTapped(index);
						--_selectedItemChanging;
					}
				}
			}
		}

		/// <summary>
		/// Handles item unselected event.
		/// </summary>
		/// <param name="sender">A native list instance from which the event has originated.</param>
		/// <param name="e">Argument associated with handler, it holds native item for which event was raised</param>
		protected void OnListViewItemUnselected(object sender, GenListItemEventArgs e)
		{
			if (_selectedItemChanging == 0)
			{
				_lastSelectedItem = null;
			}
		}

		void OnScrolled(object sender, EventArgs e)
		{
			var region = Control.CurrentRegion.ToDP();
			Element.SendScrolled(new ScrolledEventArgs(region.X, region.Y));
		}

		/// <summary>
		/// This is method handles "scroll to" requests from xamarin events.
		/// It allows for scrolling to specified item on list view.
		/// </summary>
		/// <param name="sender">A native list instance from which the event has originated.</param>
		/// <param name="e">ScrollToRequestedEventArgs.</param>
		void OnScrollToRequested(object sender, ScrollToRequestedEventArgs e)
		{
			Cell cell;
			int position;
			var scrollArgs = (ITemplatedItemsListScrollToRequestedEventArgs)e;

			var templatedItems = TemplatedItemsView.TemplatedItems;
			if (Element.IsGroupingEnabled)
			{
				var results = templatedItems.GetGroupAndIndexOfItem(scrollArgs.Group, scrollArgs.Item);
				if (results.Item1 == -1 || results.Item2 == -1)
					return;

				var group = templatedItems.GetGroup(results.Item1);
				cell = group[results.Item2];
			}
			else
			{
				position = templatedItems.GetGlobalIndexOfItem(scrollArgs.Item);
				cell = templatedItems[position];
			}

			Control.ApplyScrollTo(cell, e.Position, e.ShouldAnimate);
		}

		/// <summary>
		/// This method is called whenever something changes in list view data model.
		/// Method will not be invoked for grouping mode, but for example event with
		/// action reset will be handled here when switching between group and no-group mode.
		/// </summary>
		/// <param name="sender">TemplatedItemsList<ItemsView<Cell>, Cell>.</param>
		/// <param name="e">NotifyCollectionChangedEventArgs.</param>
		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				Cell before = null;
				if (e.NewStartingIndex + e.NewItems.Count < Element.TemplatedItems.Count)
				{
					before = Element.TemplatedItems[e.NewStartingIndex + e.NewItems.Count];
				}
				Control.AddSource(e.NewItems, before);
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				Control.Remove(e.OldItems);
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				UpdateSource();
			}
		}

		/// <summary>
		/// This method is called whenever something changes in list view data model.
		/// Method will be invoked for grouping mode, but some action can be also handled
		/// by OnCollectionChanged handler.
		/// </summary>
		/// <param name="sender">TemplatedItemsList<ItemsView<Cell>, Cell>.</param>
		/// <param name="e">NotifyCollectionChangedEventArgs.</param>
		void OnGroupedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				TemplatedItemsList<ItemsView<Cell>, Cell> itemsGroup = sender as TemplatedItemsList<ItemsView<Cell>, Cell>;
				Cell before = null;
				if (e.NewStartingIndex + e.NewItems.Count < itemsGroup.Count)
				{
					before = itemsGroup[e.NewStartingIndex + e.NewItems.Count];
				}
				Control.AddItemsToGroup(itemsGroup, e.NewItems, before);
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				Control.Remove(e.OldItems);
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				Control.ResetGroup(sender as TemplatedItemsList<ItemsView<Cell>, Cell>);
			}
		}

		/// <summary>
		/// Updates the source.
		/// </summary>
		void UpdateSource()
		{
			Control.Clear();
			UpdateHeader(false);
			Control.AddSource(Element.TemplatedItems);
			UpdateFooter(false);
			UpdateSelectedItem();
		}

		/// <summary>
		/// Updates the header.
		/// </summary>
		void UpdateHeader(bool initialize)
		{
			if (initialize)
				return;
			Control.SetHeader(((IListViewController)Element).HeaderElement as VisualElement);
		}

		/// <summary>
		/// Updates the footer.
		/// </summary>
		void UpdateFooter(bool initialize)
		{
			if (initialize)
				return;
			Control.SetFooter(((IListViewController)Element).FooterElement as VisualElement);
		}

		/// <summary>
		/// Updates the has uneven rows.
		/// </summary>
		void UpdateHasUnevenRows()
		{
			Control.SetHasUnevenRows(Element.HasUnevenRows);
		}

		/// <summary>
		/// Updates the height of the row.
		/// </summary>
		void UpdateRowHeight(bool initialize)
		{
			if (initialize)
				return;

			Control.UpdateRealizedItems();
		}

		/// <summary>
		/// Updates the is grouping enabled.
		/// </summary>
		/// <param name="initialize">If set to <c>true</c>, this method is invoked during initialization
		/// (otherwise it will be invoked only after property changes).</param>
		void UpdateIsGroupingEnabled(bool initialize)
		{
			Control.IsGroupingEnabled = Element.IsGroupingEnabled;
		}

		/// <summary>
		/// Method is used for programaticaly selecting choosen item.
		/// </summary>
		void UpdateSelectedItem()
		{
			if (Element.SelectedItem == null)
			{
				if (_lastSelectedItem != null)
				{
					_lastSelectedItem.IsSelected = false;
					_lastSelectedItem = null;
				}
				if (Control.SelectedItem != null)
				{
					Control.SelectedItem.IsSelected = false;
				}
			}
			else
			{
				var templatedItems = TemplatedItemsView.TemplatedItems;
				var results = templatedItems.GetGroupAndIndexOfItem(Element.SelectedItem);
				if (results.Item1 != -1 && results.Item2 != -1)
				{
					var itemGroup = templatedItems.GetGroup(results.Item1);
					var cell = itemGroup[results.Item2];

					++_selectedItemChanging;
					Control.ApplySelectedItem(cell);
					--_selectedItemChanging;
				}
			}
		}

		void UpdateSelectionMode()
		{
			if (Element.SelectionMode == ListViewSelectionMode.None)
			{
				Element.SelectedItem = null;
				Control.IsHighlight = false;
			}
			else
			{
				Control.IsHighlight = true;
			}
		}

		void UpdateVerticalScrollBarVisibility()
		{
			Control.VerticalScrollBarVisibility = Element.VerticalScrollBarVisibility.ToPlatform();
		}

		void UpdateHorizontalScrollBarVisibility()
		{
			Control.HorizontalScrollBarVisibility = Element.HorizontalScrollBarVisibility.ToPlatform();
		}

		void UpdateSeparator()
		{
			Control.BottomLineColor = Element.SeparatorVisibility == SeparatorVisibility.Default ? Element.SeparatorColor.ToPlatformEFL() : EColor.Transparent;
		}
	}
}
