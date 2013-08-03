﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altairis.Fakturoid.Client {
    public class FakturoidSubjectsProxy : FakturoidEntityProxy {

        internal FakturoidSubjectsProxy(FakturoidContext context) : base(context) { }

        /// <summary>
        /// Gets list of all subjects.
        /// </summary>
        /// <returns>List of <see cref="JsonSubject"/> instances.</returns>
        public IEnumerable<JsonSubject> Select() {
            return base.GetUnpagedEntities<JsonSubject>("subjects.json");
        }

        /// <summary>
        /// Selects single subject with specified ID.
        /// </summary>
        /// <param name="id">The subject id.</param>
        /// <returns>Instance of <see cref="JsonSubject"/> class.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">id;Value must be greater than zero.</exception>
        public JsonSubject SelectSingle(int id) {
            if (id < 1) throw new ArgumentOutOfRangeException("id", "Value must be greater than zero.");

            return base.GetSingleEntity<JsonSubject>(string.Format("subjects/{0}.json", id));
        }

        /// <summary>
        /// Deletes subject with specified id.
        /// </summary>
        /// <param name="id">The contact id.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">id;Value must be greater than zero.</exception>
        public void Delete(int id) {
            if (id < 1) throw new ArgumentOutOfRangeException("id", "Value must be greater than zero.");

            var c = this.Context.GetHttpClient();
            base.DeleteSingleEntity(string.Format("subjects/{0}.json", id));
        }

        /// <summary>
        /// Creates the specified new subject.
        /// </summary>
        /// <param name="entity">The new subject.</param>
        /// <returns>ID of newly created subject.</returns>
        /// <exception cref="System.ArgumentNullException">entity</exception>
        public int Create(JsonSubject entity) {
            if (entity == null) throw new ArgumentNullException("entity");

            return base.CreateEntity("subjects.json", entity);
        }

        /// <summary>
        /// Updates the specified subject.
        /// </summary>
        /// <param name="entity">The subject to update.</param>
        /// <returns>Instance of <see cref="JsonSubject"/> class with modified entity.</returns>
        /// <exception cref="System.ArgumentNullException">entity</exception>
        public JsonSubject Update(JsonSubject entity) {
            if (entity == null) throw new ArgumentNullException("entity");

            return base.UpdateSingleEntity(string.Format("subjects/{0}.json", entity.id), entity);
        }

    }
}
