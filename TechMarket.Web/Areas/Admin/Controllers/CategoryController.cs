using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechMarket.DataAccess.Data;
using TechMarket.Entities.Models;
using TechMarket.Entities.Repositories;
using TechMarket.Utilities;


namespace TechMarket.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.AdminRole)]
    public class CategoryController : Controller
    {
        private IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var categories = _unitOfWork.Category.GetAll();
            return View(categories);
        }
        [HttpGet]
        public IActionResult Create() { return View(); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(category);
                _unitOfWork.Complete();
                TempData["Create"] = "Category has Created Successfuly";
                return RedirectToAction("Index");
            }
            return View(category);

        }
        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null | id == 0)
                NotFound();

            var categoryInDb = _unitOfWork.Category.GetFirstorDefault(x => x.Id == id);
            return View(categoryInDb);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(category);
                _unitOfWork.Complete();
                TempData["Edit"] = "Category has Updated Successfuly";
                return RedirectToAction("Index");
            }
            return View(category);

        }
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null | id == 0)
                NotFound();

            var categoryInDb = _unitOfWork.Category.GetFirstorDefault(x => x.Id == id);
            return View(categoryInDb);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int? id)
        {
            var categoryInDb = _unitOfWork.Category.GetFirstorDefault(x => x.Id == id);

            if (categoryInDb == null)
                NotFound();

            _unitOfWork.Category.Remove(categoryInDb);
            _unitOfWork.Complete();
            TempData["Delete"] = "Category has deleted Successfuly";
            return RedirectToAction("Index");

        }
    }
}
