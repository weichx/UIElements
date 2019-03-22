using System.Collections.Generic;
using System.Threading.Tasks;

namespace UIForia.DataSource {

    public class Adapter<T> where T : class, IRecord {

        protected IRecordStore<T> store;
        
        public virtual Task Configure(IRecordStore<T> store) {
            this.store = store;
            return null;
        }

        public virtual void SetStore(IRecordStore<T> store) {
            this.store = store;
        }

        public virtual async Task<T> AddRecord(T record) {
            return record;
        }

        public virtual async Task<T> GetRecord(long id, T currentRecord) {
            return currentRecord;
        }

        public virtual async Task<T> SetRecord(long id, T newRecord, T oldRecord) {
            return newRecord;
        }

        public virtual async Task<ICollection<T>> LoadRecords(ICollection<T> output) {
            return store.GetAllRecords(output);
        }

        public virtual bool RecordChanged(T recordA, T recordB) {
            return recordA != recordB;
        }

        public async Task<T> RemoveRecord(long id, T localRecord) {
            return localRecord;
        }

        public virtual void Serialize() {
        }

    }

}