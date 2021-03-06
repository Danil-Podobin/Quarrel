﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Quarrel.Controls.Messages
{
    public class SelectionBindingTextBox : TextBox
    {
        public static readonly DependencyProperty BindableSelectionStartProperty =
            DependencyProperty.Register(
            "BindableSelectionStart",
            typeof(int),
            typeof(SelectionBindingTextBox),
            new PropertyMetadata(0, OnBindableSelectionStartChanged));

        public static readonly DependencyProperty BindableSelectionLengthProperty =
            DependencyProperty.Register(
            "BindableSelectionLength",
            typeof(int),
            typeof(SelectionBindingTextBox),
            new PropertyMetadata(0, OnBindableSelectionLengthChanged));

        public static readonly DependencyProperty BindableTextProperty =
            DependencyProperty.Register(
            "BindableText",
            typeof(string),
            typeof(SelectionBindingTextBox),
            new PropertyMetadata(0, OnBindableTextChanged));

        private bool changeFromUI;

        public SelectionBindingTextBox() : base()
        {
            this.SelectionChanged += this.OnSelectionChanged;
            this.TextChanged += OnTextChanged;
        }

        public int BindableSelectionStart
        {
            get
            {
                return (int)this.GetValue(BindableSelectionStartProperty);
            }

            set
            {
                this.SetValue(BindableSelectionStartProperty, value);
            }
        }

        public int BindableSelectionLength
        {
            get
            {
                return (int)this.GetValue(BindableSelectionLengthProperty);
            }

            set
            {
                this.SetValue(BindableSelectionLengthProperty, value);
            }
        }

        public string BindableText
        {
            get
            {
                return (string)this.GetValue(BindableTextProperty);
            }
            set
            {
                this.SetValue(BindableTextProperty, value);
                Text = value;
            }
        }

        private static void OnBindableSelectionStartChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var textBox = dependencyObject as SelectionBindingTextBox;

            if (!textBox.changeFromUI)
            {
                int newValue = (int)args.NewValue;
                textBox.SelectionStart = newValue;
            }
            else
            {
                textBox.changeFromUI = false;
            }
        }

        private static void OnBindableSelectionLengthChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var textBox = dependencyObject as SelectionBindingTextBox;

            if (!textBox.changeFromUI)
            {
                int newValue = (int)args.NewValue;
                textBox.SelectionLength = newValue;
            }
            else
            {
                textBox.changeFromUI = false;
            }
        }

        private static void OnBindableTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var textBox = dependencyObject as SelectionBindingTextBox;
            textBox.BindableText = (string)args.NewValue;
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (this.BindableSelectionStart != this.SelectionStart)
            {
                this.changeFromUI = true;
                this.BindableSelectionStart = this.SelectionStart;
            }

            if (this.BindableSelectionLength != this.SelectionLength)
            {
                this.changeFromUI = true;
                this.BindableSelectionLength = this.SelectionLength;
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.BindableText != this.Text)
            {
                this.BindableText = this.Text;
            }
        }
    }
}
