﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cars.Models;
using Cars.DAL;
using System.Web.Security;

namespace Cars.Controllers
{
    public class CarsController : Controller
    {
        private RentalContext db = new RentalContext();
        private static DateTime start;
        private static DateTime end;
        private static CarFilter carFilter = new CarFilter();


        // GET: Cars
        public ActionResult Index()
        {
            UpdateCars();
            //*** Cecking if theres a user connected. if yes, save information about him on session.
            string id = System.Web.HttpContext.Current.User.Identity.Name;
            if (id != "")
            {
                User authUser = db.Users.Find(id);
                SaveSession(authUser);
            }
            return View();
        }

        /// <summary>
        /// taking out of availability cars that are renting today
        /// </summary>
        private void UpdateCars()
        {
            foreach (var item in db.Rentals.ToList())
            {
                if (item.StartDate == DateTime.Today)
                {
                    Car car = db.Cars.Find(item.Car.ID);
                    car.IsAvailable = false;
                    db.SaveChanges();
                }
            }
        }

        //public ActionResult UsersList()
        //{
        //    return View(db.Users.ToList());
        //}

        /// <summary>
        /// presenting a specific car from the footer
        /// </summary>
        /// <param name="manifacturer"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult ShowCar(string manifacturer, string model)
        {
            ViewBag.years = db.CarTypes.Select(t => t.Year).Distinct();
            ViewBag.mani = db.CarTypes.Select(t => t.ManifacturerName).Distinct();
            ViewBag.model = db.CarTypes.Select(t => t.ModelName).Distinct();
            CarType carType = db.CarTypes.FirstOrDefault
                (t => t.ManifacturerName == manifacturer && t.ModelName == model);
            return View(carType);
        }

        /// <summary>
        /// presenting the fleet of the cars to the customer. per filter and page number.
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public ActionResult CarsList(int skip = 1, int take = 3)
        {
            string userId = System.Web.HttpContext.Current.User.Identity.Name;
            if (userId != "")
            {
                bool isManager = db.Users.FirstOrDefault(t => t.UserID == userId).IsManager;
                if (isManager)
                {
                    return Redirect("/cars/managercarslist");
                }
            }
            List<CarType> carTypes;
            int carsNum;
            MakeList(skip, take, out carTypes, out carsNum);
            CreateViewBags(skip, take, carsNum);
            return View(carTypes);

        }

        /// <summary>
        /// Making the list of the cars specific to the page, with the filters added
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="carTypes"></param>
        /// <param name="carsNum"></param>
        private void MakeList(int skip, int take, out List<CarType> carTypes, out int carsNum)
        {
            carTypes = new List<CarType>();
            carTypes = db.CarTypes.OrderBy(t => t.ManifacturerName).ToList();
            carTypes = FilterCars(carTypes);
            carsNum = carTypes.Count;
            carTypes = carTypes.
                Skip((skip - 1) * take).
                Take(take).
                ToList();
        }

        /// <summary>
        /// create all the information to send the client side
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="carsNum"></param>
        private void CreateViewBags(int skip, int take, int carsNum)
        {
            ViewBag.carsNum = carsNum;
            ViewBag.years = db.CarTypes.Select(t => t.Year).Distinct();
            ViewBag.mani = db.CarTypes.Select(t => t.ManifacturerName).Distinct();
            ViewBag.model = db.CarTypes.Select(t => t.ModelName).Distinct();
            ViewBag.current = skip;
            if (carsNum % take == 0)
            {
                ViewBag.pages = (carsNum / take);
            }
            else
            {
                ViewBag.pages = (carsNum / take) + 1;
            }
        }

        /// <summary>
        /// changing the list according to the filters added
        /// </summary>
        /// <param name="carTypes"></param>
        /// <returns></returns>
        private static List<CarType> FilterCars(List<CarType> carTypes)
        {
            if (carFilter.Gear != GearType.DEFAULT)
            {
                carTypes = carTypes.
                    Where(t => t.Gear == carFilter.Gear).
                    ToList();
            }
            if (carFilter.Year != 0)
            {
                carTypes = carTypes.
                    Where(w => w.Year == carFilter.Year).
                    ToList();
            }
            if (carFilter.Manifacturer != "" && carFilter.Manifacturer != null)
            {
                carTypes = carTypes.
                    Where(w => w.ManifacturerName == carFilter.Manifacturer).
                    ToList();
            }
            if (carFilter.Model != "" && carFilter.Model != null)
            {
                carTypes = carTypes.
                    Where(w => w.ModelName == carFilter.Model).
                    ToList();
            }
            if (carFilter.FreeText != "" && carFilter.FreeText != null)
            {
                carTypes = carTypes.
                    Where(s => s.ManifacturerName.ToLower().Contains(carFilter.FreeText.ToLower())
                    || (s.ModelName.ToLower().Contains(carFilter.FreeText.ToLower()))).
                    ToList();

            }

            return carTypes;
        }

        /// <summary>
        /// initializing the filters that the client picked
        /// </summary>
        /// <param name="filterGear"></param>
        /// <param name="filterYear"></param>
        /// <param name="manifacturer"></param>
        /// <param name="model"></param>
        /// <param name="freeText"></param>
        public void InitFilter(GearType filterGear, int filterYear, string manifacturer, string model, string freeText)
        {
            carFilter.Gear = filterGear;
            carFilter.Year = filterYear;
            carFilter.Manifacturer = manifacturer;
            carFilter.Model = model;
            carFilter.FreeText = freeText;
        }

        /// <summary>
        /// initializing the start hire date that the client picked
        /// </summary>
        /// <param name="startDate"></param>
        public void InitStartDate(DateTime startDate)
        {
            start = startDate;
        }

        /// <summary>
        /// initializing the end hire date that the client picked
        /// </summary>
        /// <param name="endDate"></param>
        public void InitEndDate(DateTime endDate)
        {
            end = endDate;
        }

        /// <summary>
        /// a page that shows the details of the car picked, dates and prices before purchasing
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car selectedCar = FindAvailableCar(id);

            ViewBag.carNumber = selectedCar.CarNumber;
            CarType carType;
            CarInfo carinfo;
            MakeCarInfo(id, selectedCar, out carType, out carinfo);
            if (carType == null)
            {
                return HttpNotFound();
            }
            return View(carinfo);
        }

        /// <summary>
        /// gether the car information for the rental request
        /// </summary>
        /// <param name="id"></param>
        /// <param name="selectedCar"></param>
        /// <param name="carType"></param>
        /// <param name="carinfo"></param>
        private void MakeCarInfo(int? id, Car selectedCar, out CarType carType, out CarInfo carinfo)
        {
            carType = db.CarTypes.Find(id);
            carinfo = new CarInfo
            {
                Car = selectedCar,
                CarType = carType,
                HireDateStart = start,
                HireDateEnd = end,
            };
        }

        /// <summary>
        /// finds an available car in the fleet according to the cartype and the dates asked
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Car FindAvailableCar(int? id)
        {
            Car selectedCar = new Car();
            //*** list of all unavailable cars in the specic dates:
            List<Car> unavalaibleCars =
                db.Rentals.Where(t => (t.StartDate >= start && t.StartDate <= end) ||
                (t.StartDate <= start && t.EndDate >= start)).Select(t => t.Car).ToList();
            //*** check if the requested cartype available:
            foreach (var item in db.Cars.Where(t => t.CarTypeID == id).ToList())
            {
                selectedCar = item;
                if (!unavalaibleCars.Exists(c => c.ID == selectedCar.ID))
                {
                    break;
                }
                //if the selected car exists in the unavailable list- give the car number '0' and look for a new one:
                else
                {
                    selectedCar.CarNumber = 0;
                }
            }
            return selectedCar;
        }

        public ActionResult UserDetails(string id)
        {
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [Authorize]
        public ActionResult Purchase(int? id)
        {
            string userId = System.Web.HttpContext.Current.User.Identity.Name;
            Car car = db.Cars.Find(id);
            //car.HireDateStart = start;
            //car.HireDateEnd = end;
            Rental rental = new Rental();

            rental.Car = car;
            rental.CarNumber = car.CarNumber;
            rental.StartDate = start;
            rental.EndDate = end;
            //int userId= db.Users.FirstOrDefault(t => t.UserName == name).UserID;
            rental.UserId = userId;
            //Car=car,
            Rental check = db.Rentals.FirstOrDefault(
                t => t.CarNumber == rental.CarNumber
                && t.UserId == rental.UserId
                && t.StartDate == rental.StartDate
                && t.EndDate == rental.EndDate);
            if (check != null)
            {
                return View();
            }
            db.Rentals.Add(rental);
            db.SaveChanges();
            return View();
        }

        [Authorize]
        public ActionResult Purchases(int skip = 1, int take = 3)
        {
            string userId = System.Web.HttpContext.Current.User.Identity.Name;
            //int userId = db.Users.FirstOrDefault(t => t.UserName == name).UserID;
            //if (db.Users.FirstOrDefault(t=>t.UserID==userId).IsManager)
            //{
            //    return View("ManagerOrdersList");
            //}
            List<Rental> rentals = db.Rentals.Where(t => t.UserId == userId).ToList();

            int carsNum = rentals.Count;
            rentals = rentals.
                Skip((skip - 1) * take).
                Take(take).
                ToList();
            ViewBag.carsNum = carsNum;
            ViewBag.current = skip;
            if (carsNum % take == 0)
            {
                ViewBag.pages = (carsNum / take);
            }
            else
            {
                ViewBag.pages = (carsNum / take) + 1;
            }

            return View(rentals);
        }

        [Authorize]
        public ActionResult Workers()
        {
            string userId = System.Web.HttpContext.Current.User.Identity.Name;
            User user = db.Users.FirstOrDefault(t => t.UserID == userId);
            if (user.IsManager)
            {
                return Redirect("/cars/manager");
            }
            else if (user.IsEmployee)
            {
                return Redirect("/cars/employee");
            }
            return Redirect("/cars/index");
        }

        public ActionResult employee()
        {
            return View();
        }

        public ActionResult manager()
        {
            return View();
        }

        public ActionResult ManagerCarsList()
        {
            return View(db.Cars.ToList());
        }

        public ActionResult returning(string userId, int carNumber = 0)
        {
            List<Rental> rentals = new List<Rental>();
            if (carNumber == 0)
            {
                rentals = db.Rentals.Where(t => t.UserId == userId && t.ReturningDate == null).ToList();
            }
            else
            {
                rentals = db.Rentals.Where(t => t.UserId == userId && t.CarNumber == carNumber && t.ReturningDate == null).ToList();
            }
            if (rentals.Count != 0)
            {
                //CarType carType = db.CarTypes.FirstOrDefault(t => t.CarTypeID == db.Cars.FirstOrDefault(c => c.CarNumber == carNumber).CarTypeID);
                //ViewBag.dailyPrice = carType.DailyPrice;
                //ViewBag.latePrice = carType.LateDayPrice;
                return View(rentals);
            }
            TempData["error"] = "yes";
            //ViewBag.error = "yes";
            return Redirect("/cars/employee");

        }

        [HttpPost]
        public ActionResult returning(/*Rental rental,*/ DateTime? returningDate = null, int rentalId = 0, int carNumber = 0, string userId = "0")
        {
            if (returningDate != null)
            {
                Rental rent = db.Rentals.Find(rentalId);
                Car car = db.Cars.Find(rent.Car.ID);
                rent.ReturningDate = returningDate;
                car.IsAvailable = true;
                db.SaveChanges();
                return View("Purchase");
            }
            carNumber = 0;
            return Redirect("returning/?userId=" + userId);
        }

        public ActionResult thanks()
        {
            return View();
        }

        [HttpGet]
        public ActionResult login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult login(string userName, string password, string returnUrl)
        {
            User user = db.Users.FirstOrDefault(t => t.UserName == userName && t.Password == password);

            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(user.UserID.ToString(), false);
                Session["authname"] = userName;

                if (user.IsManager)
                {
                    Session["authrole"] = "Manager";
                }
                else if (user.IsEmployee)
                {
                    Session["authrole"] = "Employee";
                }
                else
                {
                    Session["authrole"] = "Customer";
                }
                if (returnUrl == "" || returnUrl == null)
                {
                    return Redirect("/cars/index");
                }
                return Redirect(returnUrl);
            }
            return View();
        }

        public ActionResult logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return Redirect("/cars/index");
        }

        public ActionResult CreateUser()
        {
            string userId = System.Web.HttpContext.Current.User.Identity.Name;

            if (userId != "")
            {
                User user = db.Users.Find(userId);
                if (user.IsManager)
                {
                    return View("ManagerCreateUser");
                }
                return Redirect("/cars/index");
            }
            ViewBag.fromCreate = "trueee";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(User user)
        {
            User existUser = db.Users.FirstOrDefault(t => t.UserID == user.UserID || t.Email == user.Email);
            if (existUser != null)
            {
                ViewBag.exist = "exist";
                return View(user);
            }
            string userId = System.Web.HttpContext.Current.User.Identity.Name;
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                if (userId == "")
                {
                    FormsAuthentication.SetAuthCookie(user.UserID, false);
                }
                User authUser = db.Users.Find(user.UserID);
                SaveSession(authUser);
                return View("Purchase");
            }

            //ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            //ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(user);
        }

        private void SaveSession(User authUser)
        {
            //User authUser = db.Users.Find(user.UserID);
            Session["userId"] = authUser.UserID;
            Session["authname"] = authUser.UserName;
            if (authUser.IsManager)
            {
                Session["authrole"] = "Manager";
            }
            else if (authUser.IsEmployee)
            {
                Session["authrole"] = "Employee";
            }
            else
            {
                Session["authrole"] = "Customer";
            }
        }

        // GET: Cars/Create
        public ActionResult Create()
        {
            ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName");
            ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName");
            return View();
        }

        public ActionResult ManagerUsersList()
        {
            return View(db.Users.ToList());
        }

        // POST: Cars/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CarNumber,Kilometer,IsProper,IsAvailable,CarTypeID,StoreID")] Car car)
        {
            if (ModelState.IsValid)
            {
                db.Cars.Add(car);
                db.SaveChanges();
                return View("Purchase");
            }

            ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(car);
        }

        // GET: Cars/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car car = db.Cars.Find(id);
            if (car == null)
            {
                return HttpNotFound();
            }
            ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(car);
        }

        // POST: Cars/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,CarNumber,Kilometer,IsProper,IsAvailable,CarTypeID,StoreID")] Car car)
        {
            if (ModelState.IsValid)
            {
                //Car editedCar = db.Cars.Find(car.ID);
                //editedCar = car;
                db.Entry(car).State = EntityState.Modified;
                db.SaveChanges();
                return View("Purchase");
            }
            ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(car);
        }

        // GET: Cars/EditUser/5
        public ActionResult EditUser(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            //ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            //ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(user);
        }

        // POST: Cars/EditUser/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser([Bind(Include = "UserID,FullName,UserName,BirthDate,Sex,Email,Password,IsEmployee,IsManager")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return View("Purchase");
            }
            //ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            //ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(user);
        }

        // GET: Cars/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car car = db.Cars.Find(id);
            if (car == null)
            {
                return HttpNotFound();
            }
            return View(car);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Car car = db.Cars.Find(id);
            db.Cars.Remove(car);
            db.SaveChanges();
            return View("Purchase");
        }

        // GET: Cars/DeleteUser/5
        public ActionResult DeleteUser(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Cars/DeleteUser/5
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUserConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return View("Purchase");
        }

        public ActionResult ManagerOrderssList()
        {
            return View(db.Rentals.ToList());
        }

        // GET: Cars/EditOrder/5
        public ActionResult EditOrder(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rental rental = db.Rentals.Find(id);
            if (rental == null)
            {
                return HttpNotFound();
            }
            //ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            //ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(rental);
        }

        // POST: Cars/EditOrder/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditOrder(Rental rental)
        {
            if (ModelState.IsValid)
            {
                db.Entry(rental).State = EntityState.Modified;
                db.SaveChanges();
                return View("Purchase");
            }
            //ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            //ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(rental);
        }

        public ActionResult OrderDetails(int? id)
        {
            Rental rental = db.Rentals.Find(id);
            if (rental == null)
            {
                return HttpNotFound();
            }
            return View(rental);
        }

        // GET: Cars/Delete/5
        public ActionResult DeleteOrder(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rental rental = db.Rentals.Find(id);
            if (rental == null)
            {
                return HttpNotFound();
            }
            return View(rental);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("DeleteOrder")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteOrderConfirmed(int id)
        {
            Rental rental = db.Rentals.Find(id);
            db.Rentals.Remove(rental);
            db.SaveChanges();
            return View("Purchase");
        }

        public ActionResult CreateOrder()
        {
            ViewBag.users = db.Users.ToList();
            ViewBag.cars = db.Cars.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrder(Rental rental)
        {
            if (ModelState.IsValid)
            {
                db.Rentals.Add(rental);
                db.SaveChanges();
                return View("Purchase");
            }
            return View(rental);
        }

        public ActionResult ManagerCarTypesList()
        {
            return View(db.CarTypes.ToList());
        }

        // GET: Cars/EditCarType/5
        public ActionResult EditCarType(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CarType carType = db.CarTypes.Find(id);
            if (carType == null)
            {
                return HttpNotFound();
            }
            //ViewBag.CarTypeID = new SelectList(db.CarTypes, "CarTypeID", "ModelName", car.CarTypeID);
            //ViewBag.StoreID = new SelectList(db.stores, "StoreID", "StoreName", car.StoreID);
            return View(carType);
        }

        // POST: Cars/EditCarType/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCarType(CarType carType)
        {
            if (ModelState.IsValid)
            {
                db.Entry(carType).State = EntityState.Modified;
                db.SaveChanges();
                return View("Purchase");
            }
            return View(carType);
        }

        public ActionResult CarTypeDetails(int? id)
        {
            CarType carType = db.CarTypes.Find(id);
            if (carType == null)
            {
                return HttpNotFound();
            }
            return View(carType);
        }

        // GET: Cars/DeleteCarType/5
        public ActionResult DeleteCarType(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CarType carType = db.CarTypes.Find(id);
            if (carType == null)
            {
                return HttpNotFound();
            }
            return View(carType);
        }

        // POST: Cars/DeleteCarType/5
        [HttpPost, ActionName("DeleteCarType")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCarTypeConfirmed(int id)
        {
            CarType carType = db.CarTypes.Find(id);
            db.CarTypes.Remove(carType);
            db.SaveChanges();
            return View("Purchase");
        }

        public ActionResult CreateCarType()
        {
            //ViewBag.users = db.Users.ToList();
            //ViewBag.cars = db.Cars.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCarType(CarType carType)
        {
            if (ModelState.IsValid)
            {
                db.CarTypes.Add(carType);
                db.SaveChanges();
                return View("Purchase");
            }
            return View(carType);
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}
