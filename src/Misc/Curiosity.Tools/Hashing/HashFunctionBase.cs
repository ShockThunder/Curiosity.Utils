using System;
using System.IO;
using System.Text;

namespace Curiosity.Tools.Hashing
{
    /// <summary>
    /// Abstract implementation of an IHashFunction.
    /// Provides convenience checks and ensures a default HashSize has been set at construction.
    /// </summary>
    public abstract class HashFunctionBase 
    {
        public int HashSize => _hashSize;

        /// <summary>
        /// Flag to determine if a hash function needs a seekable stream in order to calculate the hash.
        /// Override to true to make <see cref="ComputeHash(Stream)" /> pass a seekable stream to <see cref="ComputeHashInternal(UnifiedData)" />.
        /// </summary>
        /// <value>
        /// <c>true</c> if a seekable stream; otherwise, <c>false</c>.
        /// </value>
        protected bool RequiresSeekableStream => false;

        private readonly int _hashSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashFunctionBase"/> class.
        /// </summary>
        /// <param name="hashSize"><inheritdoc cref="HashSize" /></param>
        protected HashFunctionBase(int hashSize)
        {
            _hashSize = hashSize;
        }

        public byte[] ComputeHash(byte[] data)
        {
            return ComputeHashInternal(new ArrayData(data));
        }

        /// <exception cref="System.ArgumentException">Stream \data\ must be readable.;data</exception>
        public byte[] ComputeHash(Stream data)
        {
            if (!data.CanRead)
                throw new ArgumentException("Stream \"data\" must be readable.", "data");

            if (!data.CanSeek && RequiresSeekableStream)
            {
                byte[] buffer;

                using (var ms = new MemoryStream())
                {
                    data.CopyTo(ms);

                    buffer = ms.ToArray();
                }

                return ComputeHashInternal(
                    new ArrayData(buffer));
            }

            return ComputeHashInternal(
                new StreamData(data));
        }

        /// <summary>
        /// Computes hash value for given stream.
        /// </summary>
        /// <param name="data">Data to hash.</param>
        /// <returns>
        /// Hash value of data as byte array.
        /// </returns>
        protected abstract byte[] ComputeHashInternal(UnifiedData data);

        /// <summary>
        /// Computes hash value for given data.
        /// </summary>
        /// <param name="data">Data to be hashed.</param>
        /// <returns>
        /// Hash value of the data as byte array.
        /// </returns>
        /// <remarks>
        /// UTF-8 encoding used to convert string to bytes.
        /// </remarks>
        public byte[] ComputeHash(string data)
        {
            return ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }
}