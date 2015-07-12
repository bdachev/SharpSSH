using System.Threading;

namespace Tamir.SharpSsh.java.lang
{
    public class JavaThread
    {
        private Thread t;

        public JavaThread(Thread t)
        {
            this.t = t;
        }

        public JavaThread(JavaRunnable r)
            : this(new Thread(new ThreadStart(r.run)))
        {
        }

        public void Name(string name)
        {
            t.Name = name;
        }

        public void Start()
        {
            t.Start();
        }

        public bool IsAlive()
        {
            return t.IsAlive;
        }

        public void Interrupt()
        {
            try
            {
                t.Interrupt();
            }
            catch
            {
            }
        }
    }
}