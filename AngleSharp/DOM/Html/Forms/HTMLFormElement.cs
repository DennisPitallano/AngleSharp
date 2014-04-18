﻿namespace AngleSharp.DOM.Html
{
    using AngleSharp.DOM.Collections;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the form element.
    /// </summary>
    [DOM("HTMLFormElement")]
    public sealed class HTMLFormElement : HTMLElement
    {
        #region Fields

        Task _plannedNavigation;
        CancellationTokenSource _cancel;
        HTMLFormControlsCollection _elements;

        #endregion

        #region ctor

        /// <summary>
        /// Creates a new HTML form element.
        /// </summary>
        internal HTMLFormElement()
        {
            _cancel = new CancellationTokenSource();
            _name = Tags.Form;
            _elements = new HTMLFormControlsCollection(this);
        }

        #endregion

        #region Index

        /// <summary>
        /// Gets the form element at the specified index.
        /// </summary>
        /// <param name="index">The index in the elements collection.</param>
        /// <returns>The element or null.</returns>
        public Element this[Int32 index]
        {
            get { return _elements[index]; }
        }

        /// <summary>
        /// Gets the form element(s) with the specified name.
        /// </summary>
        /// <param name="name">The name or id of the element.</param>
        /// <returns>A collection with elements, an element or null.</returns>
        public Object this[String name]
        {
            get { return _elements[name]; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value of the name attribute.
        /// </summary>
        [DOM("name")]
        public String Name
        {
            get { return GetAttribute("name"); }
            set { SetAttribute("name", value); }
        }

        /// <summary>
        /// Gets the number of elements in the Elements collection.
        /// </summary>
        [DOM("length")]
        public Int32 Length
        {
            get { return _elements.Length; }
        }

        /// <summary>
        /// Gets all the form controls belonging to this form element.
        /// </summary>
        [DOM("elements")]
        public HTMLFormControlsCollection Elements
        {
            get { return _elements; }
        }

        /// <summary>
        /// Gets or sets the character encodings that are to be used for the submission.
        /// </summary>
        [DOM("acceptCharset")]
        public String AcceptCharset
        {
            get { return GetAttribute("acceptCharset"); }
            set { SetAttribute("acceptCharset", value); }
        }

        /// <summary>
        /// Gets or sets the form's name within the forms collection.
        /// </summary>
        [DOM("action")]
        public String Action
        {
            get { return GetAttribute("action"); }
            set { SetAttribute("action", value); }
        }

        /// <summary>
        /// Gets or sets if autocomplete is turned on or off.
        /// </summary>
        [DOM("autocomplete")]
        public PowerState Autocomplete
        {
            get { return ToEnum(GetAttribute("autocomplete"), PowerState.On); }
            set { SetAttribute("autocomplete", value.ToString()); }
        }

        /// <summary>
        /// Gets or sets the encoding to use for sending the form.
        /// </summary>
        [DOM("enctype")]
        public String Enctype
        {
            get { return CheckEncType(GetAttribute("enctype")); }
            set { SetAttribute("enctype", CheckEncType(value)); }
        }

        /// <summary>
        /// Gets or sets the encoding to use for sending the form.
        /// </summary>
        [DOM("encoding")]
        public String Encoding
        {
            get { return Enctype; }
            set { Enctype = value; }
        }

        /// <summary>
        /// Gets or sets the method to use for transmitting the form.
        /// </summary>
        [DOM("method")]
        public HttpMethod Method
        {
            get { return ToEnum(GetAttribute("method"), HttpMethod.GET); }
            set { SetAttribute("method", value.ToString()); }
        }

        /// <summary>
        /// Gets or sets the indicator that the form is not to be validated during submission.
        /// </summary>
        [DOM("noValidate")]
        public Boolean NoValidate
        {
            get { return GetAttribute("novalidate") != null; }
            set { SetAttribute("novalidate", value ? String.Empty : null); }
        }

        /// <summary>
        /// Gets or sets the target name of the response to the request.
        /// </summary>
        [DOM("target")]
        public String Target
        {
            get { return GetAttribute("target"); }
            set { SetAttribute("target", value); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Submits the form element from the form element itself.
        /// </summary>
        /// <returns>The current form element.</returns>
        [DOM("submit")]
        public HTMLFormElement Submit()
        {
            SubmitForm(this, true);
            return this;
        }

        /// <summary>
        /// Resets the form to the previous (default) state.
        /// </summary>
        /// <returns>The current form element.</returns>
        [DOM("reset")]
        public HTMLFormElement Reset()
        {
            foreach (var element in _elements)
                element.Reset();

            return this;
        }

        /// <summary>
        /// Checks if the form is valid, i.e. if all fields fulfill their requirements.
        /// </summary>
        /// <returns>True if the form is valid, otherwise false.</returns>
        [DOM("checkValidity")]
        public Boolean CheckValidity()
        {
            foreach (var element in _elements)
                if (!element.CheckValidity())
                    return false;

            return true;
        }
        
        #endregion

        #region Helpers

        void SubmitForm(HTMLElement from, Boolean submittedFromSubmitMethod)
        {
            var formDocument = OwnerDocument;

            //TODO
            //If form document has no associated browsing context or its active
            //sandboxing flag set has its sandboxed forms browsing context flag
            //set, then abort these steps without doing anything.

            //TODO
            //var browsingContext = new object();

            if (!submittedFromSubmitMethod && from.Attributes[AttributeNames.FormNoValidate] == null && NoValidate)
            {
                if (!CheckValidity())
                {
                    FireSimpleEvent(EventNames.Invalid);
                    return;
                }
            }

            var action = Action;

            if (String.IsNullOrEmpty(action))
                action = formDocument.DocumentUri;

            if (!Location.IsAbsolute(action))
                action = Location.MakeAbsolute(from.BaseURI, action);

            //Enctype
            //Method
            //Target

            //TODO
            //If the user indicated a specific browsing context to use when submitting
            //the form, then let target browsing context be that browsing context.
            //Otherwise, apply the rules for choosing a browsing context given a browsing
            //context name using target as the name and form browsing context as the
            //context in which the algorithm is executed, and let target browsing context
            //be the resulting browsing context.

            //TODO
            //var replace = false;
            //If target browsing context was created in the previous step, or, alternatively,
            //if the form document has not yet completely loaded and the submitted from
            //submit() method is set, then let replace be true. Otherwise, let it be false

            var location = new Location(action);

            switch (location.Protocol)
            {
                case KnownProtocols.Http:
                case KnownProtocols.Https:
                    if (Method == HttpMethod.GET)
                        MutateActionUrl();
                    else if (Method == HttpMethod.POST)
                        SubmitAsEntityBody(action);
                    break;

                case KnownProtocols.Ftp:
                case KnownProtocols.JavaScript:
                    GetActionUrl();
                    break;

                case KnownProtocols.Data:
                    if (Method == HttpMethod.GET)
                        GetActionUrl();
                    else if (Method == HttpMethod.POST)
                        PostToData();
                    break;

                case KnownProtocols.Mailto:
                    if (Method == HttpMethod.GET)
                        MailWithHeaders();
                    else if (Method == HttpMethod.POST)
                        MailAsBody();
                    break;
            }
        }

        /// <summary>
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#submit-data-post
        /// </summary>
        void PostToData()
        {
        }

        /// <summary>
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#submit-mailto-headers
        /// </summary>
        void MailWithHeaders()
        {
        }

        /// <summary>
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#submit-mailto-body
        /// </summary>
        void MailAsBody()
        {
        }

        /// <summary>
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#submit-get-action
        /// </summary>
        void GetActionUrl()
        {
        }

        /// <summary>
        /// Submits the body of the form.
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#submit-body
        /// </summary>
        void SubmitAsEntityBody(String action)
        {
            var encoding = String.IsNullOrEmpty(AcceptCharset) ? OwnerDocument.CharacterSet : AcceptCharset;
            var formDataSet = ConstructDataSet();
            var enctype = Enctype;
            var mimeType = String.Empty;
            Stream result = null;

            if (enctype.Equals(MimeTypes.StandardForm, StringComparison.OrdinalIgnoreCase))
            {
                result = formDataSet.AsUrlEncoded(DocumentEncoding.Resolve(encoding));
                mimeType = MimeTypes.StandardForm;
            }
            else if (enctype.Equals(MimeTypes.MultipartForm, StringComparison.OrdinalIgnoreCase))
            {
                result = formDataSet.AsMultipart(DocumentEncoding.Resolve(encoding));
                mimeType = String.Concat(MimeTypes.MultipartForm, "; boundary=", formDataSet.Boundary);
            }
            else if (enctype.Equals(MimeTypes.Plain, StringComparison.OrdinalIgnoreCase))
            {
                result = formDataSet.AsPlaintext(DocumentEncoding.Resolve(encoding));
                mimeType = MimeTypes.Plain;
            }

            _plannedNavigation = NavigateTo(action, HttpMethod.POST, result, mimeType);
        }

        /// <summary>
        /// Plan to navigate to an action using the specified method with the given
        /// entity body of the mime type.
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#plan-to-navigate
        /// </summary>
        /// <param name="action">The action to use.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="body">The entity body of the request.</param>
        /// <param name="mime">The MIME type of the entity body.</param>
        async Task NavigateTo(String action, HttpMethod method, Stream body, String mime)
        {
            if (_plannedNavigation != null)
            {
                _cancel.Cancel();
                _plannedNavigation = null;
            }

            var stream = await _owner.Options.SendAsync(new Uri(action), body, mime, method, _cancel.Token);
            var html = _owner as HTMLDocument;

            if (html != null)
                html.Load(stream);
        }

        /// <summary>
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#submit-mutate-action
        /// </summary>
        void MutateActionUrl()
        {
        }

        FormDataSet ConstructDataSet(HTMLElement submitter = null)
        {
            var formDataSet = new FormDataSet();

            foreach (var field in _elements)
            {
                if (field.ParentElement is HTMLDataListElement)
                    continue;
                else if (field.Disabled)
                    continue;

                field.ConstructDataSet(formDataSet, submitter);
            }

            return formDataSet;
        }

        String CheckEncType(String encType)
        {
            if (encType == MimeTypes.Plain || encType == MimeTypes.MultipartForm)
                return encType;

            return MimeTypes.StandardForm;
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets if the node is in the special category.
        /// </summary>
        protected internal override Boolean IsSpecial
        {
            get { return true; }
        }

        #endregion
    }
}
