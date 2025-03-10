using Microsoft.AspNetCore.Mvc;
using TechMarket.DataAccess.Implementation;
using TechMarket.Entities.Models;
using TechMarket.Entities.Repositories;
using TechMarket.Entities.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Stripe.Checkout;

namespace TechMarket.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var orders = _unitOfWork.OrderHeader.GetAll(o => o.ApplicationUserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
            return View(orders);
        }

        public IActionResult Details(int id)
        {

            var orderHeader = _unitOfWork.OrderHeader.GetFirstorDefault(o => o.Id == id);
            if (orderHeader == null)
            {
                return NotFound();
            }


            var orderDetails = _unitOfWork.OrderDetail.GetAll(od => od.OrderHeaderId == id, Includeword: "Product");



            var orderVM = new OrderVM
            {
                OrderHeader = orderHeader,
                OrderDetails = orderDetails
            };

            return View(orderVM);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.GetFirstorDefault(o => o.Id == id);
            var orderDetails = _unitOfWork.OrderDetail.GetAll(od => od.OrderHeaderId == id);
            if (orderHeader == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error While Deleting"
                });
            }


            _unitOfWork.OrderDetail.RemoveRange(orderDetails);

            _unitOfWork.OrderHeader.Remove(orderHeader);
            _unitOfWork.Complete();
            return Json(new
            {
                success = true,
                message = "Order Deleted Successfully"
            });
        }

        public IActionResult StripePayment(int orderId)
        {
            var order = _unitOfWork.OrderHeader.GetFirstorDefault(
                 o => o.Id == orderId,
                 Includeword: "OrderDetails.Product"
            );

            if (order == null)
            {
                return NotFound();
            }

            var domain = "https://localhost:44387/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = order.OrderDetails.Select(orderDetail => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(orderDetail.Price * 100), 
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = orderDetail.Product.Name,  
                        },
                    },
                    Quantity = orderDetail.Count, 
                }).ToList(),
                Mode = "payment",
                SuccessUrl = domain + $"customer/cart/orderconfirmation?id={orderId}",
                CancelUrl = domain + $"customer/cart/index",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            order.SessionId = session.Id;
            order.PaymentIntentId = session.PaymentIntentId;

            _unitOfWork.OrderHeader.Update(order);
            _unitOfWork.Complete();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303); 
        }

    }
}

