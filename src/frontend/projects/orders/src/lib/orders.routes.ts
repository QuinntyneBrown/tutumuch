import { Routes } from '@angular/router';

/** Lazy routes of the orders feature area, mounted at /: checkout, the order confirmation, purchases and sales, the buyer's order, the seller's sale, payouts and the administrator's problem page (admin shell). */
export const routes: Routes = [
  {
    path: 'checkout/:reference',
    loadComponent: () => import('./checkout/checkout-page').then((m) => m.CheckoutPage),
    title: 'Checkout · TuTuMuch',
  },
  {
    path: 'orders/:id/confirmed',
    loadComponent: () => import('./order-confirmation/order-confirmation-page').then((m) => m.OrderConfirmationPage),
    title: 'Order confirmed · TuTuMuch',
  },
  {
    path: 'orders/:id',
    loadComponent: () => import('./order-detail/order-detail-page').then((m) => m.OrderDetailPage),
    title: 'Order · TuTuMuch',
  },
  {
    path: 'sales/:id',
    loadComponent: () => import('./sale-detail/sale-detail-page').then((m) => m.SaleDetailPage),
    title: 'Sale · TuTuMuch',
  },
  {
    path: 'account/purchases',
    loadComponent: () => import('./order-history/order-history-page').then((m) => m.OrderHistoryPage),
    title: 'Purchases · TuTuMuch',
    data: { list: 'purchases' },
  },
  {
    path: 'account/sales',
    loadComponent: () => import('./order-history/order-history-page').then((m) => m.OrderHistoryPage),
    title: 'Sales · TuTuMuch',
    data: { list: 'sales' },
  },
  {
    path: 'account/payouts',
    loadComponent: () => import('./payouts/payouts-page').then((m) => m.PayoutsPage),
    title: 'Payouts · TuTuMuch',
  },
  {
    path: 'admin/problems/:reference',
    loadComponent: () => import('./problem-detail/problem-detail-page').then((m) => m.ProblemDetailPage),
    title: 'Order problem · TuTuMuch admin',
    data: { shell: 'admin' },
  },
];
