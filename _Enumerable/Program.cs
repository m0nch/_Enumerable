using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    namespace System.Linq
    {
        class Program
        {
            static void Main(string[] args)
            {
                List<Student> students = new List<Student>
            {
            new Student() { LastName = "Doe", FirstName = "Jhon", Age = 25 },
            new Student() { LastName = "Doe", FirstName = "Jane", Age = 27 },
            new Student() { LastName = "Parker", FirstName = "Sara", Age = 21 },
            new Student() { LastName = "Simpson", FirstName = "Jessica", Age = 21 },
            new Student() { LastName = "Washington", FirstName = "Andre", Age = 21 }
            };

                List<Teacher> teachers = new List<Teacher>
            {
            new Teacher() { LastName = "Williams", FirstName = "Michael", Age = 33 },
            new Teacher() { LastName = "Anderson", FirstName = "Robert", Age = 41 },
            new Teacher() { LastName = "Wilson", FirstName = "William", Age = 44 },
            new Teacher() { LastName = "Harris", FirstName = "Richard", Age = 54 },
            new Teacher() { LastName = "Clark", FirstName = "Thomas", Age = 48 }
            };

                //GroupBy
                var query1 = students._GroupBy(student => student.Age == 21);
                var query2 = students._GroupBy(student => student.Age > 24);

                //FirstOrDefault, First
                var res1 = students._FirstOrDefault(st => st.Age > 25); //return null
                var res2 = students._First(st => st.Age > 25); //throw an Exception
                Console.WriteLine($"{res1.Age} {res2.Age}");

                //Aggregate
                var olderAge = students._Aggregate((older, next) => next.Age > older.Age ? next : older);
                Console.WriteLine($"{olderAge.FirstName} {olderAge.Age}");

                //All
                var isAdult = students._All(st => st.Age > 18);
                Console.WriteLine($"{isAdult}");

                //Avarage
                var avarageAge = students._Average(age => age.Age);
                Console.WriteLine($"{avarageAge}");

                //Select, Concat
                var query = students._Select(st => st.LastName)._Concat(teachers._Select(tch => tch.LastName));
                foreach (string name in query)
                {
                    Console.Write($"{name}, ");
                }

                //IEnumerable
                //students.Distinct()
    
            Console.ReadKey();
            }
        }
    }
    public class Student
    {
        public Student()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public int Age { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
    public class Teacher
    {
        public Teacher()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public int Age { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
}