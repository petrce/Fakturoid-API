﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Altairis.Fakturoid.Client {
    /// <summary>
    /// Proxy class for working with 
    /// </summary>
    public abstract class FakturoidEntityProxy {

        // Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="FakturoidEntityProxy"/> class.
        /// </summary>
        /// <param name="context">The related context.</param>
        /// <exception cref="System.ArgumentNullException">context</exception>
        protected FakturoidEntityProxy(FakturoidContext context) {
            if (context == null) throw new ArgumentNullException("context");
            this.Context = context;
        }

        /// <summary>
        /// Gets the related context.
        /// </summary>
        /// <value>
        /// The related context.
        /// </value>
        public FakturoidContext Context { get; private set; }

        // Helper methods for proxy classes


        /// <summary>
        /// Gets all paged entities, making sequential repeated requests for pages.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="additionalQueryParams">The additional query params.</param>
        /// <returns>All existing entities of given type.</returns>
        /// <remarks>The result may contain duplicate entities, if they are modified between requests for pages. In current version of API, there is no way to solve rhis.</remarks>
        protected IEnumerable<T> GetAllPagedEntities<T>(string baseUri, object additionalQueryParams = null) {
            var completeList = new List<T>();
            var page = 1;

            while (true) {
                var partialList = this.GetPagedEntities<T>(baseUri, page, additionalQueryParams);
                if (!partialList.Any()) break; // no more entities
                completeList.AddRange(partialList);
                page++;
            }

            return completeList;
        }

        /// <summary>
        /// Gets single page of entities.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="page">The page number.</param>
        /// <param name="additionalQueryParams">The additional query params.</param>
        /// <returns>Paged list of entities of given type.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        /// <exception cref="System.ArgumentException">Value cannot be empty or whitespace only string.;uri</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">page;Page must be greater than zero.</exception>
        /// <remarks>The number of entities on single page is determined by API and is different for each type. In current version of API, there is no way to detect or change page size.</remarks>
        protected IEnumerable<T> GetPagedEntities<T>(string baseUri, int page, object additionalQueryParams = null) {
            if (baseUri == null) throw new ArgumentNullException("uri");
            if (string.IsNullOrWhiteSpace(baseUri)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "uri");
            if (page < 1) throw new ArgumentOutOfRangeException("page", "Page must be greater than zero.");

            // Build URI
            var uri = baseUri + "?page=" + page + GetQueryStringFromParams(additionalQueryParams, "&");

            // Get result
            return this.GetSingleEntity<IEnumerable<T>>(uri);
        }

        /// <summary>
        /// Gets the unpaged entities.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="additionalQueryParams">The additional query params.</param>
        /// <returns>List of entities of given type.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        /// <exception cref="System.ArgumentException">Value cannot be empty or whitespace only string.;uri</exception>
        protected IEnumerable<T> GetUnpagedEntities<T>(string baseUri, object additionalQueryParams = null) {
            if (baseUri == null) throw new ArgumentNullException("uri");
            if (string.IsNullOrWhiteSpace(baseUri)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "uri");

            // Build URI
            var uri = baseUri + GetQueryStringFromParams(additionalQueryParams, "?");

            // Get result
            return this.GetSingleEntity<IEnumerable<T>>(uri);
        }

        /// <summary>
        /// Gets single entity of given type.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns>Single entity of given type.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        /// <exception cref="System.ArgumentException">Value cannot be empty or whitespace only string.;uri</exception>
        protected T GetSingleEntity<T>(string uri) {
            if (uri == null) throw new ArgumentNullException("uri");
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "uri");

            // Get result
            var c = this.Context.GetHttpClient();
            var r = c.GetAsync(uri).Result;

            // Ensure result was successfull
            r.EnsureFakturoidSuccess();

            // Parse and return result
            return r.Content.ReadAsAsync<T>().Result;
        }


        /// <summary>
        /// Creates new entity.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="uri">The endpoint URI.</param>
        /// <param name="newEntity">The new entity.</param>
        /// <returns>ID of newly created entity.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// uri
        /// or
        /// newEntity
        /// </exception>
        /// <exception cref="System.ArgumentException">Value cannot be empty or whitespace only string.;uri</exception>
        /// <exception cref="System.FormatException"></exception>
        protected int CreateEntity<T>(string uri, T newEntity) {
            if (uri == null) throw new ArgumentNullException("uri");
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "uri");
            if (newEntity == null) throw new ArgumentNullException("newEntity");

            // Create new entity
            var c = this.Context.GetHttpClient();
            var r = c.PostAsJsonAsync<T>(uri, newEntity).Result;
            r.EnsureFakturoidSuccess();

            // Extract ID from URI
            try {
                var idString = r.Headers.Location.ToString();
                if (idString.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) idString = idString.Substring(0, idString.Length - 5); // remove .json extension
                idString = idString.Substring(idString.LastIndexOf('/') + 1); // last path component should now be numeric ID
                return int.Parse(idString);
            }
            catch (Exception) {
                throw new FormatException(string.Format("Unexpected format of new entity URI. Expected format 'scheme://anystring/123456.json', got '{0}' instead.", r.Headers.Location));
            }
        }

        /// <summary>
        /// Deletes single entity.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        /// <exception cref="System.ArgumentException">Value cannot be empty or whitespace only string.;uri</exception>
        protected void DeleteSingleEntity(string uri) {
            if (uri == null) throw new ArgumentNullException("uri");
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "uri");

            // Get result
            var c = this.Context.GetHttpClient();
            var r = c.DeleteAsync(uri).Result;

            // Ensure result was successfull
            r.EnsureFakturoidSuccess();
        }

        /// <summary>
        /// Updates the single entity.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="uri">The entity URI.</param>
        /// <param name="entity">The entity object.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// uri
        /// or
        /// entity
        /// </exception>
        /// <exception cref="System.ArgumentException">Value cannot be empty or whitespace only string.;uri</exception>
        protected T UpdateSingleEntity<T>(string uri, T entity) {
            if (uri == null) throw new ArgumentNullException("uri");
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "uri");
            if (entity == null) throw new ArgumentNullException("entity");
            
            // Get result
            var c = this.Context.GetHttpClient();
            var r = c.PutAsJsonAsync(uri, entity).Result;

            // Ensure result was successfull
            r.EnsureFakturoidSuccess();

            // Return updated entity
            return r.Content.ReadAsAsync<T>().Result;
        }

        // Helper methods for this class

        private static string GetQueryStringFromParams(object queryParams, string prefix) {
            if (prefix == null) throw new ArgumentNullException("prefix");
            if (queryParams == null) return string.Empty;

            var qsb = new StringBuilder();

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(queryParams)) {
                // Get property value
                var rawValue = descriptor.GetValue(queryParams);
                if (rawValue == null) continue; // null queryParams do not propagate to query

                string stringValue = null;
                if (rawValue.GetType() == typeof(DateTime)) {
                    // Format date
                    stringValue = XmlConvert.ToString((DateTime)rawValue, XmlDateTimeSerializationMode.RoundtripKind);
                }
                else if (rawValue.GetType() == typeof(DateTime?)) {
                    // Format nullable date
                    var dateValue = (DateTime?)rawValue;
                    if (dateValue.HasValue) stringValue = XmlConvert.ToString(dateValue.Value, XmlDateTimeSerializationMode.RoundtripKind);
                }
                else {
                    var formattableValue = rawValue as IFormattable;
                    if (formattableValue != null) {
                        // Format IFormattable rawValue
                        stringValue = formattableValue.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else {
                        // Format other value - just use ToString()
                        stringValue = rawValue.ToString();
                    }
                }

                if (string.IsNullOrWhiteSpace(stringValue)) continue; // empty value after string conversion
                qsb.AppendFormat("{0}={1}&", descriptor.Name, Uri.EscapeDataString(stringValue));
            }

            var qs = qsb.ToString().TrimEnd('&');
            if (string.IsNullOrWhiteSpace(qs)) return string.Empty;
            return prefix + qs;
        }

    }
}
