using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace L2Trace
{
    public class BlockQueue<T>
    {
        public readonly int SizeLimit = 0;
        private Queue<T> _inner_queue = null;
        public int Count
        {
            get
            {
                lock (this._inner_queue)
                    return _inner_queue.Count;
            }
        }
        private ManualResetEvent _enqueue_wait = null;
        private ManualResetEvent _dequeue_wait = null;
        public BlockQueue(int sizeLimit)
        {
            this.SizeLimit = sizeLimit;
            this._inner_queue = new Queue<T>(this.SizeLimit);
            this._enqueue_wait = new ManualResetEvent(false);
            this._dequeue_wait = new ManualResetEvent(false);
        }
        public void EnQueue(T item)
        {
            if (this._IsShutdown == true) throw new InvalidCastException("Queue was shutdown. Enqueue was not allowed.");
            while (true)
            {
                lock (this._inner_queue)
                {
                    if (this._inner_queue.Count < this.SizeLimit)
                    {
                        this._inner_queue.Enqueue(item);
                        this._enqueue_wait.Reset();
                        this._dequeue_wait.Set();
                        break;
                    }
                }
                this._enqueue_wait.WaitOne();
            }
        }
        public T DeQueue()
        {
            while (true)
            {
                if (this._IsShutdown == true)
                {
                    lock (this._inner_queue) return this._inner_queue.Dequeue();
                }
                lock (this._inner_queue)
                {
                    if (this._inner_queue.Count > 0)
                    {
                        T item = this._inner_queue.Dequeue();
                        this._dequeue_wait.Reset();
                        this._enqueue_wait.Set();
                        return item;
                    }
                }
                this._dequeue_wait.WaitOne();
            }
        }
        private bool _IsShutdown = false;
        public void Shutdown()
        {
            this._IsShutdown = true;
            this._dequeue_wait.Set();
        }
    }



    public class RingBuff
    {
        private byte[] ringBuff;
        private int nextWritePos;
        private int nextReadPos;
        private int buffSize;
        private int capcity;

        public int GetBufferUsedSize()
        {
            return buffSize - capcity;
        }
        public RingBuff(int _size)
        {
            capcity = buffSize = _size;
            ringBuff = new byte[buffSize];
            nextWritePos = 0;
            nextReadPos = 0;
        }

        /// <summary>
        /// 写入缓存
        /// </summary>
        /// <param name="_buff">要写入的数据</param>
        /// <returns>是否写入</returns>
        public bool WriteBuff(byte[] _buff)
        {
            bool ret = false;
            int bsize = _buff.Length;
            if (capcity < bsize)
            {
                ret = false;
            }
            else
            {
                int rightLeft = buffSize - nextWritePos;
                //need reture to head
                if (bsize > rightLeft)
                {
                    Array.Copy(_buff, 0, ringBuff, nextWritePos, rightLeft);
                    Array.Copy(_buff, rightLeft, ringBuff, 0, bsize - rightLeft);
                    nextWritePos = bsize - rightLeft;
                }
                else if (bsize == rightLeft)
                {
                    Array.Copy(_buff, 0, ringBuff, nextWritePos, bsize);
                    nextWritePos = 0;
                }
                else
                {
                    Array.Copy(_buff, 0, ringBuff, nextWritePos, bsize);
                    nextWritePos += bsize;
                }
                capcity = capcity - bsize;
                ret = true;
            }
            return ret;
        }
        /// <summary>
        /// 写入缓存，指定读取源的长度.
        /// </summary>
        /// <param name="_buff"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public bool WriteBuff(byte[] _buff, int len)
        {
            int bsize = len;// _buff.Length;
            bsize = len > _buff.Length ? _buff.Length : bsize;
            if (capcity < bsize)
            {
                return false;
            }
            else
            {
                int rightLeft = buffSize - nextWritePos;
                //need reture to head
                if (bsize > rightLeft)
                {
                    Array.Copy(_buff, 0, ringBuff, nextWritePos, rightLeft);
                    Array.Copy(_buff, rightLeft, ringBuff, 0, bsize - rightLeft);
                    nextWritePos = bsize - rightLeft;
                }
                else if (bsize == rightLeft)
                {
                    Array.Copy(_buff, 0, ringBuff, nextWritePos, bsize);
                    nextWritePos = 0;
                }
                else
                {
                    Array.Copy(_buff, 0, ringBuff, nextWritePos, bsize);
                    nextWritePos += bsize;
                }
                capcity -= bsize;
                return true;
            }
        }

        public int ReadBuff(ref byte[] readbuff, bool MovePosition)
        {
            int len = readbuff.Length;
            int enableread = buffSize - capcity;
            Array.Clear(readbuff, 0, len);
            if (len <= 0 || enableread <= 0)
            {
                return 0;
            }
            else
            {
                int realRead = enableread >= len ? len : enableread;
                int rightLeft = buffSize - nextReadPos;
                if (realRead > rightLeft)
                {
                    Array.Copy(ringBuff, nextReadPos, readbuff, 0, rightLeft);
                    Array.Copy(ringBuff, 0, readbuff, rightLeft, realRead - rightLeft);
                    if (MovePosition)
                    {
                        nextReadPos = realRead - rightLeft;
                    }
                }
                else if (realRead == rightLeft)
                {
                    Array.Copy(ringBuff, nextReadPos, readbuff, 0, realRead);
                    if (MovePosition)
                    {
                        nextReadPos = 0;
                    }
                }
                else
                {
                    Array.Copy(ringBuff, nextReadPos, readbuff, 0, realRead);
                    if (MovePosition)
                    {
                        nextReadPos += realRead;
                    }
                }
                if (MovePosition)
                    capcity += realRead;
                return realRead;
            }
        }

        public string GetBuffInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(System.Threading.Thread.CurrentThread.Name + ":writePositon:" + nextWritePos.ToString());
            sb.Append(".  readPosition:" + nextReadPos.ToString());
            sb.Append(".  size:");
            sb.Append(buffSize);
            sb.Append("   .enable read:" + (buffSize - capcity).ToString() + ".enable write:" + capcity.ToString());
            sb.Append(System.Environment.NewLine);

            sb.Append("1-10 byte: ");

            for (int i = 0; i < 10; i++)
            {
                sb.Append(ringBuff[i].ToString("x") + "|");
            }

            return sb.ToString();
        }
        private RingBuff()
        { }
    }


    public class Utility
    {
        public UInt32 ReadStreamByte(DataStream stream, Boolean MovePosition)
        {
            byte[] data = new byte[1];
            stream.data.ReadBuff(ref data, MovePosition);
            return data[0];
        }

        public UInt32 ReadStream32(DataStream stream, Boolean MovePosition)
        {
            byte[] data = new byte[4];
            stream.data.ReadBuff(ref data, MovePosition);
            return (UInt32)(data[0] | data[1] << 8 | data[2] << 16 | data[3] << 24);
        }

        public UInt32 ReadStream16(DataStream stream, Boolean MovePosition)
        {
            byte[] data = new byte[2];
            stream.data.ReadBuff(ref data, MovePosition);
            return (UInt32)(data[0] | data[1] << 8);
        }

        public byte[] ReadStreamArray(DataStream stream, UInt32 length, Boolean MovePosition)
        {
            byte[] data = new byte[length];
            stream.data.ReadBuff(ref data, MovePosition);
            return data;
        }
    }
}
