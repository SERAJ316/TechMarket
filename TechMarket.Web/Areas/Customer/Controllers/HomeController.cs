using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TechMarket.Entities.Models;
using TechMarket.Entities.Repositories;
using TechMarket.Entities.ViewModels;
using TechMarket.Utilities;
using System.Security.Claims;
using X.PagedList;
using X.PagedList.Extensions;

namespace TechMarket.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitofwork;

        public HomeController(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public IActionResult Index(int? page)
        {
            var PageNumber = page ?? 1;
            int PageSize = 12;


            var products = _unitofwork.Product.GetAll().ToPagedList(PageNumber, PageSize);
            return View(products);
        }

        public IActionResult Details(int ProductId)
        {
            ShoppingCart obj = new ShoppingCart()
            {
                ProductId = ProductId,
                Product = _unitofwork.Product.GetFirstorDefault(v => v.Id == ProductId, Includeword: "Category"),
                Count = 1
            };
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCart.ApplicationUserId = claim.Value;

            ShoppingCart Cartobj = _unitofwork.ShoppingCart.GetFirstorDefault(
                u => u.ApplicationUserId == claim.Value && u.ProductId == shoppingCart.ProductId);

            if (Cartobj == null)
            {
                _unitofwork.ShoppingCart.Add(shoppingCart);
                _unitofwork.Complete();
                HttpContext.Session.SetInt32(SD.SessionKey,
                    _unitofwork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value).ToList().Count()
                   );

            }
            else
            {
                _unitofwork.ShoppingCart.IncreaseCount(Cartobj, shoppingCart.Count);
                _unitofwork.Complete();
            }


            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BuyNow(int productId, string returnUrl = null)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity", returnUrl = Url.Action("Details", "Home", new { productId }) });
            }

            var product = _unitofwork.Product.GetFirstorDefault(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var cartObj = _unitofwork.ShoppingCart.GetFirstorDefault(
                u => u.ApplicationUserId == claim.Value && u.ProductId == productId
            );

            if (cartObj == null)
            {
                var shoppingCart = new ShoppingCart
                {
                    ProductId = productId,
                    ApplicationUserId = claim.Value,
                    Count = 1
                };
                _unitofwork.ShoppingCart.Add(shoppingCart);
            }
            else
            {
                _unitofwork.ShoppingCart.IncreaseCount(cartObj, 1);
            }

            _unitofwork.Complete();

            return RedirectToAction("Summary", "Cart");
        }
    }
}
