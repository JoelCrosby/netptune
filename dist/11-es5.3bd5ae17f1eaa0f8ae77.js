function _classCallCheck(e,t){if(!(e instanceof t))throw new TypeError("Cannot call a class as a function")}function _defineProperties(e,t){for(var n=0;n<t.length;n++){var a=t[n];a.enumerable=a.enumerable||!1,a.configurable=!0,"value"in a&&(a.writable=!0),Object.defineProperty(e,a.key,a)}}function _createClass(e,t,n){return t&&_defineProperties(e.prototype,t),n&&_defineProperties(e,n),e}(window.webpackJsonp=window.webpackJsonp||[]).push([[11],{Vl0R:function(e,t,n){"use strict";n.r(t),n.d(t,"SettingsModule",(function(){return w}));var a=n("SVse"),c=n("PCNd"),o=n("iInd"),i=n("EMFo"),l=n("BnVf"),r=n("8Y7J"),s=n("DQLy"),u=n("O13u"),f=n("Q2Ze"),b=n("ZTz/"),p=n("s7LF"),h=n("UhP/");function m(e,t){if(1&e&&(r.Ub(0,"mat-option",5),r.Bc(1),r.Tb()),2&e){var n=t.$implicit;r.mc("value",n.value),r.Cb(1),r.Dc(" ",n.label," ")}}function g(e,t){if(1&e){var n=r.Vb();r.Sb(0),r.Ub(1,"div",1),r.Ub(2,"mat-form-field",2),r.Ub(3,"mat-label"),r.Bc(4,"Theme"),r.Tb(),r.Ub(5,"mat-select",3),r.cc("selectionChange",(function(e){return r.tc(n),r.gc().onThemeSelect(e)})),r.zc(6,m,2,2,"mat-option",4),r.Tb(),r.Tb(),r.Tb(),r.Rb()}if(2&e){var a=t.ngIf,c=r.gc();r.Cb(5),r.mc("ngModel",a.theme),r.Cb(1),r.mc("ngForOf",c.themes)}}var d,v,C,y=[{path:"**",component:(d=function(){function e(t){_classCallCheck(this,e),this.store=t,this.themes=[{value:"DEFAULT-THEME",label:"Light"},{value:"DARK-THEME",label:"Dark"},{value:"CORPORATE-THEME",label:"Corporate"}]}return _createClass(e,[{key:"ngOnInit",value:function(){this.settings$=this.store.select(i.b)}},{key:"onThemeSelect",value:function(e){var t=e.value;this.store.dispatch(Object(l.a)({theme:t}))}}]),e}(),d.\u0275fac=function(e){return new(e||d)(r.Ob(s.h))},d.\u0275cmp=r.Ib({type:d,selectors:[["app-settings-index"]],decls:3,vars:3,consts:[[4,"ngIf"],["fxLayout","column","fxLayoutAlign","start start","fxLayoutGap","gappx"],["appearance","outline",1,"mb-3","col-lg-4","col-md-6","col-sm-12","no-gutters","px-0"],["placeholder","Select Theme",3,"ngModel","selectionChange"],[3,"value",4,"ngFor","ngForOf"],[3,"value"]],template:function(e,t){1&e&&(r.Ub(0,"app-page-container"),r.zc(1,g,7,2,"ng-container",0),r.hc(2,"async"),r.Tb()),2&e&&(r.Cb(1),r.mc("ngIf",r.ic(2,1,t.settings$)))},directives:[u.a,a.l,f.c,f.g,b.a,p.k,p.n,a.k,h.n],pipes:[a.b],styles:[""]}),d)}],T=((v=function e(){_classCallCheck(this,e)}).\u0275mod=r.Mb({type:v}),v.\u0275inj=r.Lb({factory:function(e){return new(e||v)},imports:[[o.i.forChild(y)],o.i]}),v),k=n("Fk/C"),w=((C=function e(){_classCallCheck(this,e)}).\u0275mod=r.Mb({type:C}),C.\u0275inj=r.Lb({factory:function(e){return new(e||C)},imports:[[a.c,c.a,k.a,T]]}),C)}}]);