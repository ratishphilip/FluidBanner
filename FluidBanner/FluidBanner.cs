// Copyright (c) 2016 Ratish Philip 
//
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions: 
// 
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software. 
// 
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE. 
//
// This file is part of the FluidBanner project: 
// https://github.com/ratishphilip/FluidBanner
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using CompositionExpressionToolkit;
using CompositionProToolkit.Common;
using Microsoft.UI.Composition.Toolkit;

namespace FluidBanner
{
    public sealed class FluidBanner : Panel
    {
        #region Constants

        public const int DefaultDecodeWidth = 0;
        public const int DefaultDecodeHeight = 0;
        public const float ScaleDownFactor = 0.7f;
        public const float HoverScaleFactor = 1.1f;
        public const double DefaultItemGap = 30;
        public const float TargetOpacity = 0f;
        public static TimeSpan DefaultOpacityAnimationDuration = TimeSpan.FromMilliseconds(900);
        public static TimeSpan DefaultScaleAnimationDuration = TimeSpan.FromMilliseconds(900);
        public static TimeSpan InsetClipAnimationDuration = TimeSpan.FromMilliseconds(600);
        public static TimeSpan InsetAnimationDuration = TimeSpan.FromMilliseconds(500);
        public static TimeSpan InsetAnimationDelayDuration = TimeSpan.FromMilliseconds(500);
        public static readonly Color DefaultItemBackground = Colors.Black;

        #endregion

        #region Fields

        private Compositor _compositor;
        private CompositionImageFactory _imageFactory;
        private ContainerVisual _rootContainer;
        private LayerVisual _bgLayer;
        private LayerVisual _topLayer;
        private ImplicitAnimationCollection _implicitAnimationCollection;
        private ContainerVisual _selectedVisual;
        private ContainerVisual _hoverVisual;
        private Dictionary<ContainerVisual, Rect> _fluidItems;
        private List<CompositionImage> _images;
        private Rect _bannerBounds;
        private float _itemWidth;
        private float _itemHeight;
        private float _availableWidth;
        private float _availableHeight;
        private bool _isExpanding;
        private bool _isCollapsing;
        // Animations
        private KeyFrameAnimation<float> _expandLeftInset;
        private KeyFrameAnimation<float> _expandRightInset;
        private KeyFrameAnimation<float> _expandInsetClip;
        private KeyFrameAnimation<float> _collapseLeftInset;
        private KeyFrameAnimation<float> _collapseRightInset;
        private KeyFrameAnimation<float> _collapseInsetClip;

        #endregion

        #region Dependency Properties

        #region DecodeHeight

        /// <summary>
        /// DecodeHeight Dependency Property
        /// </summary>
        public static readonly DependencyProperty DecodeHeightProperty =
            DependencyProperty.Register("DecodeHeight", typeof(int), typeof(FluidBanner),
                new PropertyMetadata(DefaultDecodeHeight));

        /// <summary>
        /// Gets or sets the DecodeHeight property. This dependency property 
        /// indicates the height, in pixels, that the images are decoded to.
        /// </summary>
        public int DecodeHeight
        {
            get { return (int)GetValue(DecodeHeightProperty); }
            set { SetValue(DecodeHeightProperty, value); }
        }

        #endregion

        #region DecodeWidth

        /// <summary>
        /// DecodeWidth Dependency Property
        /// </summary>
        public static readonly DependencyProperty DecodeWidthProperty =
            DependencyProperty.Register("DecodeWidth", typeof(int), typeof(FluidBanner),
                new PropertyMetadata(DefaultDecodeWidth));

        /// <summary>
        /// Gets or sets the DecodeWidth property. This dependency property 
        /// indicates the width, in pixels, that the images are decoded to.
        /// </summary>
        public int DecodeWidth
        {
            get { return (int)GetValue(DecodeWidthProperty); }
            set { SetValue(DecodeWidthProperty, value); }
        }

        #endregion

        #region ItemBackground

        /// <summary>
        /// ItemBackground Dependency Property
        /// </summary>
        public static readonly DependencyProperty ItemBackgroundProperty =
            DependencyProperty.Register("ItemBackground", typeof(Color), typeof(FluidBanner),
                new PropertyMetadata(DefaultItemBackground, OnItemBackgroundChanged));

        /// <summary>
        /// Gets or sets the ItemBackground property. This dependency property 
        /// indicates the color for the background of the item.
        /// </summary>
        public Color ItemBackground
        {
            get { return (Color)GetValue(ItemBackgroundProperty); }
            set { SetValue(ItemBackgroundProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ItemBackground property.
        /// </summary>
        /// <param name="d">FluidBanner</param>
		/// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnItemBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (FluidBanner)d;
            panel.OnItemBackgroundChanged();
        }

        /// <summary>
        /// Provides the class instance an opportunity to handle changes to the ItemBackground property.
        /// </summary>
        private void OnItemBackgroundChanged()
        {
            // Refresh Layout
            InvalidateArrange();
        }

        #endregion

        #region ItemGap

        /// <summary>
        /// ItemGap Dependency Property
        /// </summary>
        public static readonly DependencyProperty ItemGapProperty =
            DependencyProperty.Register("ItemGap", typeof(double), typeof(FluidBanner),
                new PropertyMetadata(DefaultItemGap, OnItemGapChanged));

        /// <summary>
        /// Gets or sets the ItemGap property. This dependency property 
        /// indicates the gap between adjacent items in the banner.
        /// </summary>
        public double ItemGap
        {
            get { return (double)GetValue(ItemGapProperty); }
            set { SetValue(ItemGapProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ItemGap property.
        /// </summary>
        /// <param name="d">FluidBanner</param>
		/// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnItemGapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (FluidBanner)d;
            panel.OnItemGapChanged();
        }

        /// <summary>
        /// Provides the class instance an opportunity to handle changes to the ItemGap property.
        /// </summary>
        private void OnItemGapChanged()
        {
            // Refresh Layout
            InvalidateArrange();
        }

        #endregion

        #region ItemsSource

        /// <summary>
        /// ItemsSource Dependency Property
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<Uri>), typeof(FluidBanner),
                new PropertyMetadata(null, OnItemsSourceChanged));

        /// <summary>
        /// Gets or sets the ItemsSource property. This dependency property 
        /// indicates the collection of Uris of images to be shown in the banner.
        /// </summary>
        public IEnumerable<Uri> ItemsSource
        {
            get { return (IEnumerable<Uri>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ItemsSource property.
        /// </summary>
        /// <param name="d">FluidBanner</param>
		/// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (FluidBanner)d;
            panel.OnItemsSourceChanged();
        }

        /// <summary>
        /// Provides the class instance an opportunity to handle changes to the ItemsSource property.
        /// </summary>
        private void OnItemsSourceChanged()
        {
            // Clear previous banner items
            Children.Clear();

            // Create the CompositionImages based on the new URIs
            CreateImages();

            // Refresh Layout
            InvalidateArrange();
        }

        #endregion

        #region Padding

        /// <summary>
        /// Padding Dependency Property
        /// </summary>
        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register("Padding", typeof(Thickness), typeof(FluidBanner),
                new PropertyMetadata(new Thickness(), OnPaddingChanged));

        /// <summary>
        /// Gets or sets the Padding property. This dependency property 
        /// indicates the Padding thickness for the content.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Padding property.
        /// </summary>
        /// <param name="d">FluidBanner</param>
		/// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (FluidBanner)d;
            panel.OnPaddingChanged();
        }

        /// <summary>
        /// Provides the class instance an opportunity to handle changes to the Padding property.
        /// </summary>
        private void OnPaddingChanged()
        {
            InvalidateArrange();
        }

        #endregion

        #endregion

        #region Construction / Initialization

        /// <summary>
        /// Ctor
        /// </summary>
        public FluidBanner()
        {
            _fluidItems = new Dictionary<ContainerVisual, Rect>();
            _images = new List<CompositionImage>();
            Loaded += OnBannerLoaded;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Handles the Measure pass during Layout
        /// </summary>
        /// <param name="availableSize">Available size</param>
        /// <returns>Total Size required to accommodate all the Children</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Compositor will be null the very first time
            if (_compositor == null)
            {
                InitializeComposition();
            }

            return availableSize;
        }

        /// <summary>
        /// Handles the Arrange pass during Layout
        /// </summary>
        /// <param name="finalSize">Final Size of the control</param>
        /// <returns>Total size occupied by all the Children</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if ((ItemsSource == null) || (!ItemsSource.Any()))
                return finalSize;

            var padding = Padding;
            var paddingSize = padding.CollapseThickness();
            var width = finalSize.Width;
            var height = finalSize.Height;
            var itemCount = ItemsSource.Count();

            _availableWidth = (width - paddingSize.Width).Single();
            _availableHeight = (height - paddingSize.Height).Single();
            var gapCount = itemCount - 1;
            var totalGap = (gapCount * ItemGap).Single();
            var availableNonGapWidth = _availableWidth - totalGap;
            var availableNonGapHeight = _availableHeight;

            _itemWidth = availableNonGapWidth / itemCount;
            _itemHeight = availableNonGapHeight;
            _bannerBounds = new Rect(padding.Left, padding.Top, _availableWidth, _availableHeight);

            // Check if any visual is currently selected. If yes then obtain its index.
            // The new visual at that index should be selected and added to the toplayer
            var selectedVisualIndex = -1;
            if (_selectedVisual != null)
            {
                foreach (var item in _fluidItems)
                {
                    selectedVisualIndex++;
                    if (ReferenceEquals(item.Key, _selectedVisual))
                        break;
                }
            }

            // Clear previous visuals
            _fluidItems.Clear();
            _rootContainer.Children.RemoveAll();
            _rootContainer.Size = finalSize.ToVector2();

            // Background Layer
            _bgLayer = _compositor.CreateLayerVisual();
            _bgLayer.Size = _rootContainer.Size;
            _rootContainer.Children.InsertAtBottom(_bgLayer);
            _bgLayer.CenterPoint = new Vector3(_bgLayer.Size * 0.5f, 0);

            // Top Layer
            _topLayer = _compositor.CreateLayerVisual();
            _topLayer.Size = _rootContainer.Size;
            _rootContainer.Children.InsertAtTop(_topLayer);

            // Create Visuals
            for (var i = 0; i < itemCount; i++)
            {
                // Add a visual for the background
                var bgVisual = _compositor.CreateSpriteVisual();
                bgVisual.Size = new Vector2(_availableWidth, _availableHeight);
                bgVisual.Brush = _compositor.CreateColorBrush(ItemBackground);

                // Create the visual for the content
                var contentVisual = _compositor.CreateSpriteVisual();
                contentVisual.Offset = Vector3.Zero;
                contentVisual.Size = new Vector2(_availableWidth, _availableHeight);

                // Load image from the Uri
                contentVisual.Brush = _compositor.CreateSurfaceBrush(_images.ElementAt(i).Surface);

                // Create the container for the content
                var container = _compositor.CreateContainerVisual();
                container.Offset = new Vector3((float)padding.Left, (float)padding.Top, 0);
                container.Size = new Vector2(_availableWidth, _availableHeight);

                // Calculate the Inset Clip
                var left = i * (_itemWidth + ItemGap.Single());
                var right = _availableWidth - (left + _itemWidth);
                container.Properties.InsertScalar("LeftInset", left);
                container.Properties.InsertScalar("RightInset", right);
                // If the current value of 'i' same as the index of the previously selected visual
                // then the InsetClip value should be appropriately set
                container.Clip = (i == selectedVisualIndex) ? 
                    _compositor.CreateInsetClip(0, 0, 0, 0) : _compositor.CreateInsetClip(left, 0, right, 0);

                // Center Point
                container.CenterPoint = new Vector3(left + (_itemWidth / 2f), (float)padding.Top + (_itemHeight / 2f), 0);
                contentVisual.CenterPoint = container.CenterPoint;
                container.ImplicitAnimations = _implicitAnimationCollection;
                contentVisual.ImplicitAnimations = _implicitAnimationCollection;

                // Set the container content
                container.Children.InsertAtTop(contentVisual);
                container.Children.InsertAtBottom(bgVisual);

                // Is the current value of 'i' same as the index of the previously selected visual
                if (i == selectedVisualIndex)
                {
                    // Add to the Top Layer
                    _topLayer.Children.InsertAtTop(container);
                    _selectedVisual = container;
                }
                else
                {
                    // Add to Background Layer
                    _bgLayer.Children.InsertAtTop(container);
                }

                // Add Visual to the fluid items
                _fluidItems[container] = new Rect(padding.Left + left, padding.Top, _itemWidth, _itemHeight);
            }

            // If there was a previously selected visual then Scale back 
            // and fade the other visuals in the banner
            if (selectedVisualIndex > -1)
            {
                // 
                foreach (var child in _bgLayer.Children)
                {
                    child.Scale = new Vector3(ScaleDownFactor, ScaleDownFactor, 1);
                    child.Opacity = TargetOpacity;
                }
            }

            // Update Animations' keyframes
            _expandRightInset.InsertKeyFrame(1f, _availableWidth - _itemWidth);
            _collapseInsetClip.InsertKeyFrame(1f, _availableWidth - _itemWidth);

            // Add the rootContainer to the visual tree
            ElementCompositionPreview.SetElementChildVisual(this, _rootContainer);

            return base.ArrangeOverride(finalSize);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle the event when the control is loaded.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">RoutedEventArgs</param>
        private void OnBannerLoaded(object sender, RoutedEventArgs e)
        {
            // Track the pointer pressed event
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
        }

        /// <summary>
        /// Handles the PointerMoved event
        /// </summary>
        /// <param name="sender">FluidBanner</param>
        /// <param name="e">PointerRoutedEventArgs</param>
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if ((ItemsSource == null) || (!ItemsSource.Any()) ||
                _isExpanding || _isCollapsing || _selectedVisual != null)
                return;

            var point = e.GetCurrentPoint(this).Position;

            HandlePointerPosition(point);
        }

        /// <summary>
        /// Handles the PointerPressed event
        /// </summary>
        /// <param name="sender">FluidBanner</param>
        /// <param name="e">PointerRoutedEventArgs</param>
        private async void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if ((ItemsSource == null) || (!ItemsSource.Any()) ||
                _isExpanding || _isCollapsing)
                return;

            var point = e.GetCurrentPoint(this).Position;

            if (_selectedVisual == null)
            {
                _selectedVisual = _fluidItems.Where(t => t.Value.Contains(point))
                                             .Select(t => t.Key)
                                             .FirstOrDefault();

                if (_selectedVisual != null)
                {
                    ResetHover(_hoverVisual);
                    _hoverVisual = null;
                    await SelectBannerItem();
                }
            }
            else
            {
                if (_bannerBounds.Contains(point))
                {
                    ResetBannerItems();
                }
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Initialize all Composition related stuff here (Compositor, Animations etc)
        /// </summary>
        private void InitializeComposition()
        {
            var rootVisual = ElementCompositionPreview.GetElementVisual(this);
            // Compositor
            _compositor = rootVisual.Compositor;

            // Final Value Expressions
            var vector3Expr = _compositor.CreateFinalValueExpression<Vector3>();
            var scalarExpr = _compositor.CreateFinalValueExpression<float>();

            // Opacity Animation
            var opacityAnimation = _compositor.CreateKeyFrameAnimation<float>()
                                              .HavingDuration(DefaultOpacityAnimationDuration)
                                              .ForTarget(() => rootVisual.Opacity);
            opacityAnimation.InsertExpressionKeyFrame(1f, scalarExpr);

            // Scale Animation
            var scaleAnimation = _compositor.CreateKeyFrameAnimation<Vector3>()
                                            .HavingDuration(DefaultScaleAnimationDuration)
                                            .ForTarget(() => rootVisual.Scale);
            scaleAnimation.InsertExpressionKeyFrame(1f, vector3Expr);

            // ImplicitAnimation
            _implicitAnimationCollection = _compositor.CreateImplicitAnimationCollection();
            _implicitAnimationCollection["Opacity"] = opacityAnimation.Animation;
            _implicitAnimationCollection["Scale"] = scaleAnimation.Animation;

            // Expand Animations
            _expandLeftInset = _compositor.CreateKeyFrameAnimation<float>()
                                          .HavingDuration(InsetAnimationDuration)
                                          .DelayBy(InsetAnimationDelayDuration);

            _expandLeftInset.InsertKeyFrame(1f, 0);

            _expandRightInset = _compositor.CreateKeyFrameAnimation<float>()
                                           .HavingDuration(InsetAnimationDuration)
                                           .DelayBy(InsetAnimationDelayDuration);

            _expandInsetClip = _compositor.CreateKeyFrameAnimation<float>()
                                          .HavingDuration(InsetClipAnimationDuration);

            _expandInsetClip.InsertKeyFrame(1f, 0);

            // Collapse Animations
            _collapseLeftInset = _compositor.CreateKeyFrameAnimation<float>()
                                            .HavingDuration(InsetAnimationDuration);

            _collapseRightInset = _compositor.CreateKeyFrameAnimation<float>()
                                             .HavingDuration(InsetAnimationDuration);

            _collapseInsetClip = _compositor.CreateKeyFrameAnimation<float>()
                                            .HavingDuration(InsetClipAnimationDuration);

            // Root Container
            _rootContainer = _compositor.CreateContainerVisual();

            // Image Factory
            _imageFactory = CompositionImageFactory.CreateCompositionImageFactory(_compositor);

            // If the ItemsSource has any items then create their corresponding images
            CreateImages();
        }

        /// <summary>
        /// Creates CompositionImages based on the URIs in ItemsSource
        /// </summary>
        private void CreateImages()
        {
            _images.Clear();
            var options = new CompositionImageOptions
            {
                DecodeWidth = DecodeWidth,
                DecodeHeight = DecodeHeight
            };

            if ((ItemsSource == null) || !ItemsSource.Any())
                return;

            foreach (var uri in ItemsSource)
            {
                _images.Add(_imageFactory.CreateImageFromUri(uri, options));
            }
        }

        /// <summary>
        /// Checks if the current Pointer location is above any of the FluidBanner items
        /// and takes appropriate action
        /// </summary>
        /// <param name="point">Current Pointer location</param>
        private void HandlePointerPosition(Point point)
        {
            ContainerVisual visual = _fluidItems.Where(t => t.Value.Contains(point))
                                                .Select(t => t.Key)
                                                .FirstOrDefault();


            // Pointer not on any visual
            if (visual == null)
            {
                if (_hoverVisual != null)
                {
                    ResetHover(_hoverVisual);
                    _hoverVisual = null;
                }
            }
            else
            {
                if (ReferenceEquals(_hoverVisual, visual))
                    return;

                if (_hoverVisual != null)
                {
                    ResetHover(_hoverVisual);
                }

                _hoverVisual = visual;
                Hover(_hoverVisual);
            }
        }

        /// <summary>
        /// Handles the Hover animation for the given visual
        /// </summary>
        /// <param name="visual">Visual to animate</param>
        private void Hover(ContainerVisual visual)
        {
            if (visual == null)
                return;

            var topIndex = visual.Children.Count - 1;
            var child = visual.Children.ElementAt(topIndex);
            if (child != null)
            {
                child.Scale = new Vector3(HoverScaleFactor, HoverScaleFactor, 1);
            }
        }

        /// <summary>
        /// Handles the Reset Hover animation for the given visual
        /// </summary>
        /// <param name="visual">Visual to animate</param>
        private void ResetHover(ContainerVisual visual)
        {
            if (visual == null)
                return;

            var topIndex = visual.Children.Count - 1;
            var child = visual.Children.ElementAt(topIndex);
            if (child != null)
            {
                child.Scale = Vector3.One;
            }
        }

        /// <summary>
        /// Performs the required animations on the selected Visual
        /// </summary>
        /// <returns>Task</returns>
        private async Task SelectBannerItem()
        {
            if (_selectedVisual == null)
                return;
            _isExpanding = true;

            // Bring the selected visual from background layer to the top layer
            _bgLayer.Children.Remove(_selectedVisual);
            _topLayer.Children.InsertAtTop(_selectedVisual);

            // Scale back and fade the remaining visuals in the banner
            foreach (var child in _bgLayer.Children)
            {
                child.Scale = new Vector3(ScaleDownFactor, ScaleDownFactor, 1);
                child.Opacity = TargetOpacity;
            }

            // If the first visual is selected, then no need to move it to the leftmost
            // position as it is already there. Just do the expand animation.
            if (ReferenceEquals(_selectedVisual, _fluidItems.Keys.ElementAt(0)))
            {
                // Delay the expand animation by the DefaultScaleAnimationDuration 
                // so that the other visuals in the Banner scale down
                await Task.Delay((int)DefaultScaleAnimationDuration.TotalMilliseconds);

                _compositor.CreateScopedBatch(CompositionBatchTypes.Animation,
                    () =>
                    {
                        // Expand the visual to occupy the full size of the banner
                        _selectedVisual.Clip.StartAnimation("RightInset", _expandInsetClip.Animation);
                    },
                    () =>
                    {
                        _isExpanding = false;
                    });

            }
            else
            {
                _compositor.CreateScopedBatch(CompositionBatchTypes.Animation,
                    () =>
                    {
                        // Animate the visual to move to the left most position
                        _selectedVisual.Clip.StartAnimation("LeftInset", _expandLeftInset.Animation);
                        _selectedVisual.Clip.StartAnimation("RightInset", _expandRightInset.Animation);
                    },
                    () =>
                    {
                        _compositor.CreateScopedBatch(CompositionBatchTypes.Animation,
                            () =>
                            {
                                // Expand the visual to occupy the full size of the banner
                                _selectedVisual.Clip.StartAnimation("RightInset", _expandInsetClip.Animation);
                            },
                            () =>
                            {
                                _isExpanding = false;
                            });

                    });
            }
        }

        /// <summary>
        /// Performs to the required animation to reset selection on the Selected Visual
        /// </summary>
        private void ResetBannerItems()
        {
            if (_selectedVisual == null)
                return;

            _isCollapsing = true;

            // Get the value to set for the LeftInset of the InsetClip
            float left;
            _selectedVisual.Properties.TryGetScalar("LeftInset", out left);
            _collapseLeftInset.InsertKeyFrame(1f, left);

            // Get the value to set for the RightInset of the InsetClip
            float right;
            _selectedVisual.Properties.TryGetScalar("RightInset", out right);
            _collapseRightInset.InsertKeyFrame(1f, right);

            // Use a scoped batch helper to queue up animations one after the other
            _compositor.CreateScopedBatch(CompositionBatchTypes.Animation,
            () =>
            {
                // Collapse the visual
                _selectedVisual.Clip.StartAnimation("RightInset", _collapseInsetClip.Animation);
            },
            async () =>
            {
                // If the first visual is selected, then no need to move it to the leftmost
                // position as it is already there.
                if (!ReferenceEquals(_selectedVisual, _fluidItems.Keys.ElementAt(0)))
                {
                    // Move the visual back to its original location in the banner
                    _selectedVisual.Clip.StartAnimation("LeftInset", _collapseLeftInset.Animation);
                    _selectedVisual.Clip.StartAnimation("RightInset", _collapseRightInset.Animation);
                }

                // Fade in the other visuals and scale them to normal size
                foreach (var child in _bgLayer.Children)
                {
                    child.Scale = Vector3.One;
                    child.Opacity = 1;
                }

                // Delay the exchange, of selected visual, between the top and background layers
                // by the DefaultScaleAnimationDuration so that other visuals in 
                // the Banner scale back to normal
                await Task.Delay((int)DefaultScaleAnimationDuration.TotalMilliseconds);

                // Bring the selected visual from top layer to the background layer
                _topLayer.Children.Remove(_selectedVisual);
                _bgLayer.Children.InsertAtTop(_selectedVisual);

                _selectedVisual = null;
                _isCollapsing = false;

                // Check the current location of the Pointer and whether it is hovering
                // over any of the FluidBanner items
                HandlePointerPosition(GetPointerPosition());
            });
        }

        /// <summary>
        /// Gets the current location of the Pointer
        /// </summary>
        /// <returns>Point</returns>
        private Point GetPointerPosition()
        {
            var currentWindow = Window.Current;

            Point point;

            try
            {
                point = currentWindow.CoreWindow.PointerPosition;
            }
            // Accessing PointerPosition might throw UnauthorizedAccessException 
            // when the computer is locked
            catch (UnauthorizedAccessException)
            {
                return new Point(double.NegativeInfinity, double.NegativeInfinity);
            }

            // Get the current window's bounds
            var bounds = currentWindow.Bounds;

            // Get the location of the FluidBanner with respect to the Window content
            GeneralTransform coordinate = TransformToVisual(currentWindow.Content);
            var location = coordinate.TransformPoint(new Point(0, 0));

            // Convert the point from global coordinates to the FluidBanner coordinates
            var result = new Point(point.X - bounds.X - location.X, point.Y - bounds.Y - location.Y);

            return result;
        }


        #endregion
    }
}
