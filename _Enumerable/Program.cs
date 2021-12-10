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
                    new Student() { LastName = "Danil", FirstName = "Jane", Age = 27 },
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

                List<string> fruits = new List<string>
                                    { "apple", "passionfruit", "banana", "mango",
                                    "orange", "blueberry", "grape", "strawberry" };

                ArrayList arrayList = new ArrayList
                                    { "Yerevan", "Gyumri", "Vanadzor", "Vagharshapat",
                                    "Abovyan", "Kapan", "Hrazdan", "Artashat", "Armavir", "Dilijan" };

                string[] array = new string[] { "Yerevan", "Gyumri", "Vanadzor", "Vagharshapat", "Abovyan", "Kapan", "Hrazdan", "Artashat", "Armavir", "Dilijan" };

                List<string> emptyList = new List<string>();
                List<int> numList = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                List<int> oddNumList = new List<int> { 1, 3, 5, 7, 9, 11, 13 };
                List<int> evenNumList = new List<int> { 2, 4, 6, 8, 10, 12, 14 };

                //Count
                //Returns the number of elements in a sequence.
                Print("Count");
                int countS = students._Count(st => st.Age == 21);
                int countT = teachers._Count(tch => tch.Age < 40);
                Console.WriteLine($"Students {countS}, Teachers {countT}");

                //GroupBy
                //Groups the elements of a sequence.
                Print("GroupBy");
                var query1 = students._GroupBy(student => student.Age == 21);
                var query2 = students._GroupBy(student => student.Age > 24);
                foreach (var item in query1)
                {
                    Console.Write($"{item._Count()}, ");
                }
                Console.WriteLine();
                foreach (var item in query2)
                {
                    Console.Write($"{item._Count()}, ");
                }
                Console.WriteLine();

                //FirstOrDefault, First
                //Returns the first element of a sequence, or a default value if no element is found.
                //Returns the first element of a sequence.
                Print("FirstOrDefault, First");
                var res1 = students._FirstOrDefault(st => st.Age > 25); //return null
                var res2 = students._First(st => st.Age > 25); //throw an Exception
                Console.WriteLine($"{res1.Age} {res2.Age}");

                //Aggregate
                //Applies an accumulator function over a sequence.
                Print("Aggregate");
                var olderAge = students._Aggregate((older, next) => next.Age > older.Age ? next : older);
                Console.WriteLine($"{olderAge.FirstName} {olderAge.Age}");

                //All
                //Determines whether all elements of a sequence satisfy a condition.
                Print("All");
                var isAdult = students._All(st => st.Age > 18);
                Console.WriteLine($"{isAdult}");

                //Avarage
                //Computes the average of a sequence of numeric values.
                Print("Avarage");
                var avarageAge = students._Average(age => age.Age);
                Console.WriteLine($"{avarageAge}");

                //Select, Concat
                //Projects each element of a sequence into a new form.
                //Concatenates two sequences.
                Print("Select, Concat");
                var query = students._Select(st => st.LastName)._Concat(teachers._Select(tch => tch.LastName));
                foreach (string name in query)
                {
                    Console.Write($"{name}, ");
                }
                Console.WriteLine();

                //Where
                Print("Where");
                IEnumerable<string> query0 = fruits._Where(fruit => fruit.Length < 6);
                foreach (string fruit in query0)
                {
                    Console.WriteLine(fruit);
                }

                //Distinct
                //Returns distinct elements from a sequence.
                Print("Distinct");
                IEnumerable<string> distinctLastNames = query._Distinct();
                Console.WriteLine("\nDistinct LastNames:");
                foreach (string lastName in distinctLastNames)
                {
                    Console.WriteLine(lastName);
                }

                //Any
                //Determines whether any element of a sequence exists or satisfies a condition.
                Print("Any");
                bool hasElements = teachers._Any();
                Console.WriteLine("The list {0} empty.",
                    hasElements ? "is not" : "is");

                //AsEnumerable
                //Returns the input typed as IEnumerable<T>.
                Print("AsEnumerable");
                var query3 = array._AsEnumerable()._Where(str => str.Contains("A"));
                foreach (var ele in query3)
                {
                    Console.WriteLine(ele);
                }

                //Cast, OrderBy
                //Casts the elements of an IEnumerable to the specified type.
                //Sorts the elements of a sequence in ascending order.
                Print("Cast, OrderBy, Select");
                IEnumerable<string> query4 = arrayList._Cast<string>()._OrderBy(city => city)._Select(city => city);
                foreach (string city in query4)
                {
                    Console.WriteLine(city);
                }
                // The following code, without the cast, doesn't compile.
                //IEnumerable<string> query5 = arrayList.OrderBy(city => city).Select(city => city);

                //Contains
                //Determines whether a sequence contains a specified element.
                Print("Contains");
                var city1 = arrayList[4];
                if (array._Contains(city1))
                {
                    Console.WriteLine(city1);
                }

                //DefaultIfEmpty
                //Returns the elements of an IEnumerable<T>, or a default valued singleton collection if the sequence is empty.
                Print("DefaultIfEmpty");
                var res3 = emptyList._DefaultIfEmpty();
                Console.WriteLine(res3);
                var res4 = oddNumList._DefaultIfEmpty();
                Console.WriteLine(res4);

                //ElementAt
                //Returns the element at a specified index in a sequence.
                Print("ElementAt");
                var teachers1 = teachers._ElementAt(1);
                Console.WriteLine(teachers1.FirstName);

                //ElementAtOrDefault
                //Returns the element at a specified index in a sequence or a default value if the index is out of range.
                Print("ElementAtOrDefault");
                var teachers2 = teachers._ElementAtOrDefault(7);
                Console.WriteLine($"{0}", teachers2 != null ? teachers2.FirstName : null);
                
                //Except
                //Produces the set difference of two sequences.
                Print("Except");
                var res5 = numList._Except(oddNumList);
                foreach (var item in res5)
                {
                    Console.Write($"{item}, ");
                }
                Console.WriteLine();

                //Intersect
                //Produces the set intersection of two sequences.
                //Produces the set intersection of two sequences according to a specified key selector function.
                Print("Intersect");
                IEnumerable<int> res6 = numList._Intersect(evenNumList);
                foreach (int item in res6)
                {
                    Console.Write($"{item}, ");
                }
                Console.WriteLine();

                //Last, LastOrDefault
                //Returns the last element of a sequence.
                //Returns the last element of a sequence, or a default value if no element is found.
                Print("Last, LastOrDefault");
                var res7 = students._LastOrDefault(st => st.Age > 25); //return null
                var res8 = students._Last(st => st.Age < 22); //throw an Exception
                Console.WriteLine($"{res7.Age} {res8.Age}");

                //OrderByDescending
                //Sorts the elements of a sequence in descending order.
                Print("OrderByDescending");
                var query5 = teachers._OrderByDescending(tch => tch.Age > 0);
                foreach (Teacher item in query5)
                {
                    Console.Write($"{item.Age}, ");
                }
                Console.WriteLine();

                //Max
                //Min
                //Returns the maximum value in a sequence of values.
                //Returns the minimum value in a sequence of values.
                Print("Max, Min");
                var query6 = teachers._Max(tch => tch.Age);
                var query7 = teachers._Min(tch => tch.Age);
                Console.WriteLine($"{query6} {query7}");

                //GroupJoin
                //Correlates the elements of two sequences based on key equality, and groups the results.
                Print("GroupJoin");

                //TODO:

                //Join
                //LongCount
                //OfType
                //Reverse
                //SelectMany
                //SequenceEqual
                //Single
                //SingleOrDefault
                //Skip
                //SkipWhile
                //Sum
                //Take
                //TakeWhile
                //ToArray
                //ToDictionary
                //ToHashSet
                //ToList
                //ToLookup
                //Union
                //Zip

                Console.ReadKey();
            }
            public static void Print(string title)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(new string('*', 10));
                Console.Write(title);
                Console.Write(new string('*', 10));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
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