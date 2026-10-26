import {
  ApplicationRef,
  EnvironmentInjector,
  Injectable,
  Injector,
  Type,
  afterNextRender,
  createComponent,
  inject,
} from '@angular/core';

import { DIALOG_DATA, DialogRef } from './dialog-ref';

/**
 * Opens a component as a modal dialog at the end of body (design system: Dialog). The rest of
 * the page becomes inert, Escape closes, focus moves to the element marked data-autofocus (or
 * the first control) and returns to the element that opened it on close.
 */
@Injectable({ providedIn: 'root' })
export class DialogService {
  private readonly appRef = inject(ApplicationRef);
  private readonly environmentInjector = inject(EnvironmentInjector);

  open<C, D = unknown, R = unknown>(component: Type<C>, data?: D): DialogRef<R> {
    const opener = document.activeElement as HTMLElement | null;
    const madeInert: Element[] = [];
    const onKeydown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') ref.close();
    };

    const ref = new DialogRef<R>(() => {
      document.removeEventListener('keydown', onKeydown);
      this.appRef.detachView(componentRef.hostView);
      componentRef.destroy();
      madeInert.forEach((element) => element.removeAttribute('inert'));
      opener?.focus();
    });

    const injector = Injector.create({
      providers: [
        { provide: DialogRef, useValue: ref },
        { provide: DIALOG_DATA, useValue: data },
      ],
      parent: this.environmentInjector,
    });
    const componentRef = createComponent(component, {
      environmentInjector: this.environmentInjector,
      elementInjector: injector,
    });
    const host = componentRef.location.nativeElement as HTMLElement;

    for (const element of Array.from(document.body.children)) {
      if (!element.hasAttribute('inert')) {
        element.setAttribute('inert', '');
        madeInert.push(element);
      }
    }
    this.appRef.attachView(componentRef.hostView);
    document.body.appendChild(host);
    document.addEventListener('keydown', onKeydown);
    afterNextRender(
      () => {
        const target =
          host.querySelector<HTMLElement>('[data-autofocus]') ??
          host.querySelector<HTMLElement>('button, [href], input, select, textarea');
        target?.focus();
      },
      { injector: this.environmentInjector },
    );
    return ref;
  }
}
