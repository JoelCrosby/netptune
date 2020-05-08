function _slicedToArray(t,e){return _arrayWithHoles(t)||_iterableToArrayLimit(t,e)||_unsupportedIterableToArray(t,e)||_nonIterableRest()}function _nonIterableRest(){throw new TypeError("Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.")}function _unsupportedIterableToArray(t,e){if(t){if("string"==typeof t)return _arrayLikeToArray(t,e);var n=Object.prototype.toString.call(t).slice(8,-1);return"Object"===n&&t.constructor&&(n=t.constructor.name),"Map"===n||"Set"===n?Array.from(t):"Arguments"===n||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)?_arrayLikeToArray(t,e):void 0}}function _arrayLikeToArray(t,e){(null==e||e>t.length)&&(e=t.length);for(var n=0,a=new Array(e);n<e;n++)a[n]=t[n];return a}function _iterableToArrayLimit(t,e){if("undefined"!=typeof Symbol&&Symbol.iterator in Object(t)){var n=[],a=!0,i=!1,s=void 0;try{for(var r,c=t[Symbol.iterator]();!(a=(r=c.next()).done)&&(n.push(r.value),!e||n.length!==e);a=!0);}catch(o){i=!0,s=o}finally{try{a||null==c.return||c.return()}finally{if(i)throw s}}return n}}function _arrayWithHoles(t){if(Array.isArray(t))return t}function _classCallCheck(t,e){if(!(t instanceof e))throw new TypeError("Cannot call a class as a function")}function _defineProperties(t,e){for(var n=0;n<e.length;n++){var a=e[n];a.enumerable=a.enumerable||!1,a.configurable=!0,"value"in a&&(a.writable=!0),Object.defineProperty(t,a.key,a)}}function _createClass(t,e,n){return e&&_defineProperties(t.prototype,e),n&&_defineProperties(t,n),t}(window.webpackJsonp=window.webpackJsonp||[]).push([[2],{IfTP:function(t,e,n){"use strict";n.r(e),n.d(e,"ProjectTasksModule",(function(){return pe}));var a=n("PCNd"),i=n("Yml6"),s=n("DQLy"),r=n("iInd"),c=function(t){return t[t.New=0]="New",t[t.Complete=1]="Complete",t[t.InProgress=2]="InProgress",t[t.OnHold=3]="OnHold",t[t.UnAssigned=4]="UnAssigned",t[t.AwaitingClassification=5]="AwaitingClassification",t[t.InActive=6]="InActive",t}({}),o=n("s7LF"),l=n("iELJ"),d=Object(s.n)("[ProjectTasks] Clear State"),u=Object(s.n)("[ProjectTasks] Load ProjectTasks"),b=Object(s.n)("[ProjectTasks] Load ProjectTasks Success",Object(s.s)()),p=Object(s.n)("[ProjectTasks] Load ProjectTasks Fail",Object(s.s)()),g=Object(s.n)("[ProjectTasks] Create Project Task",Object(s.s)()),k=Object(s.n)("[ProjectTasks] Create Project Task Success",Object(s.s)()),m=Object(s.n)("[ProjectTasks] Create Project Task Fail",Object(s.s)()),h=Object(s.n)("[ProjectTasks] Edit Project Task",Object(s.s)()),f=Object(s.n)("[ProjectTasks] Edit Project Task Success",Object(s.s)()),O=Object(s.n)("[ProjectTasks] Edit Project Task Fail",Object(s.s)()),C=Object(s.n)("[ProjectTasks] Delete Project Task",Object(s.s)()),j=Object(s.n)("[ProjectTasks] Delete Project Task Success",Object(s.s)()),v=Object(s.n)("[ProjectTasks] Delete Project Task Fail",Object(s.s)()),P=Object(s.n)("[ProjectTasks] Select Task",Object(s.s)()),y=Object(s.n)("[ProjectTasks] Clear selected Task"),T=Object(s.n)("[ProjectTasks] Set Inline Edit Active",Object(s.s)()),_=n("IIIA"),M=n("quSY"),w=n("cahD"),x=n("S1Dn"),U=n("jOI6"),I=n("8Y7J"),S=n("Q2Ze"),D=n("e6WT"),A=n("ZTz/"),F=n("UhP/"),$=n("SVse"),E=n("Dxy4");function B(t,e){if(1&t&&(I.Ub(0,"mat-option",11),I.Bc(1),I.Tb()),2&t){var n=e.$implicit;I.mc("value",n),I.Cb(1),I.Cc(n.name)}}function N(t,e){1&t&&(I.Ub(0,"mat-form-field",2),I.Ub(1,"mat-label"),I.Bc(2,"Enter Task Description"),I.Tb(),I.Pb(3,"input",12),I.Tb())}var z,L=((z=function(){function t(e,n,a){_classCallCheck(this,t),this.store=e,this.dialogRef=n,this.data=a,this.projects$=this.store.select(x.a),this.currentWorkspace$=this.store.select(w.a),this.currentProject$=this.store.select(x.b),this.subs=new M.a,this.showDescriptionField=!1,this.projectFromGroup=new o.e({nameFormControl:new o.c("",[o.q.required,o.q.minLength(4)]),projectFormControl:new o.c,descriptionFormControl:new o.c}),a&&(this.task=a)}return _createClass(t,[{key:"ngOnInit",value:function(){var t=this;this.store.dispatch(Object(U.h)()),this.task?(this.name.setValue(this.task.name),this.project.setValue(this.task.projectId),this.description.setValue(this.task.description)):(this.projectFromGroup.reset(),this.subs.add(this.currentProject$.subscribe((function(e){t.project.setValue(e)}))))}},{key:"ngOnDestroy",value:function(){this.subs.unsubscribe()}},{key:"close",value:function(){this.dialogRef.close()}},{key:"selectProject",value:function(){var t=this.project.value;this.store.dispatch(Object(_.a)({project:t}))}},{key:"getResult",value:function(){var t=this;this.subs.add(this.currentWorkspace$.subscribe((function(e){t.store.dispatch(g({task:{name:t.name.value,description:t.description.value,workspace:e.slug,projectId:t.project.value.id,assigneeId:void 0,assignee:void 0,status:c.New,sortOrder:0}})),t.dialogRef.close()})))}},{key:"name",get:function(){return this.projectFromGroup.get("nameFormControl")}},{key:"description",get:function(){return this.projectFromGroup.get("descriptionFormControl")}},{key:"project",get:function(){return this.projectFromGroup.get("projectFormControl")}}]),t}()).\u0275fac=function(t){return new(t||z)(I.Ob(s.h),I.Ob(l.f),I.Ob(l.a,8))},z.\u0275cmp=I.Ib({type:z,selectors:[["app-task-dialog"]],decls:26,vars:7,consts:[["mat-dialog-title",""],[3,"formGroup"],["appearance","outline",1,"w-100"],["matInput","","id","name","formControlName","nameFormControl","placeholder","What do you need to get done?","name","name","aria-label","With textarea","required","",1,"form-control"],["id","type","name","type","placeholder","Project","formControlName","projectFormControl",3,"selectionChange"],[3,"value",4,"ngFor","ngForOf"],["class","w-100","appearance","outline",4,"ngIf"],["mat-dialog-actions","","align","end"],["mat-stroked-button","",3,"click"],[1,"mr-2"],["mat-flat-button","","color","primary","type","button",3,"click"],[3,"value"],["matInput","","id","description","placeholder","Task Summary","formControlName","descriptionFormControl","name","Description",1,"form-control"]],template:function(t,e){1&t&&(I.Ub(0,"h1",0),I.Bc(1),I.Tb(),I.Ub(2,"form",1),I.Ub(3,"mat-form-field",2),I.Ub(4,"mat-label"),I.Bc(5,"Enter Task Summary"),I.Tb(),I.Pb(6,"textarea",3),I.Ub(7,"mat-hint"),I.Bc(8,"Required"),I.Tb(),I.Tb(),I.Pb(9,"br"),I.Ub(10,"mat-form-field",2),I.Ub(11,"mat-label"),I.Bc(12,"Task Project"),I.Tb(),I.Ub(13,"mat-select",4),I.cc("selectionChange",(function(){return e.selectProject()})),I.Ub(14,"mat-option"),I.Bc(15,"None"),I.Tb(),I.zc(16,B,2,2,"mat-option",5),I.hc(17,"async"),I.Tb(),I.Tb(),I.Pb(18,"br"),I.zc(19,N,4,0,"mat-form-field",6),I.Tb(),I.Ub(20,"div",7),I.Ub(21,"button",8),I.cc("click",(function(){return e.close()})),I.Bc(22,"Close"),I.Tb(),I.Pb(23,"div",9),I.Ub(24,"button",10),I.cc("click",(function(){return e.getResult()})),I.Bc(25),I.Tb(),I.Tb()),2&t&&(I.Cb(1),I.Cc(e.task?"Edit Task":"Add new Task"),I.Cb(1),I.mc("formGroup",e.projectFromGroup),I.Cb(14),I.mc("ngForOf",I.ic(17,5,e.projects$)),I.Cb(3),I.mc("ngIf",e.showDescriptionField),I.Cb(6),I.Dc(" ",e.task?"Save Changes":"Save Task"," "))},directives:[l.g,o.r,o.l,o.f,S.c,S.g,D.b,o.b,o.k,o.d,o.p,S.f,A.a,F.n,$.k,$.l,l.c,E.b],pipes:[$.b],styles:["Mat-Form-Field[_ngcontent-%COMP%]{margin-bottom:1.4rem;width:100%}"]}),z),R=n("bm5G"),G=n("DRwZ"),q={loading:!1},W=Object(G.a)(),V=W.getInitialState({loading:!1,loaded:!1,loadingNewTask:!1,deleteState:q,editState:q}),Y=Object(s.o)("tasks"),H=W.getSelectors().selectAll,J=Object(s.q)(Y,H),Q=Object(s.q)(J,(function(t){return t.filter((function(t){return t.status===c.Complete})).sort((function(t,e){return t.sortOrder-e.sortOrder}))})),Z=Object(s.q)(J,(function(t){return t.filter((function(t){return t.status===c.New})).sort((function(t,e){return t.sortOrder-e.sortOrder}))})),X=Object(s.q)(J,(function(t){return t.filter((function(t){return t.status===c.InActive})).sort((function(t,e){return t.sortOrder-e.sortOrder}))})),K=(Object(s.q)(Y,(function(t){return t.loading})),Object(s.q)(Y,(function(t){return t.loaded}))),tt=Object(s.q)(Y,(function(t){return t.selectedTask})),et=Object(s.q)(Y,(function(t){return t.inlineEditActive})),nt=n("zHaW"),at=n("O13u"),it=n("ltgo"),st=n("4/QA"),rt=n("fcPU"),ct=n("XNiG"),ot=n("xgIS"),lt=n("itXk"),dt=n("UXun"),ut=n("1G5W"),bt=n("gcYM"),pt=n("vkgz"),gt=n("SxV6"),kt=n("Tj54"),mt=n("pMoy"),ht=["taskInlineContainer"],ft=["taskInlineForm"],Ot=["taskInput"];function Ct(t,e){if(1&t){var n=I.Vb();I.Sb(0),I.Ub(1,"button",4),I.cc("click",(function(){return I.tc(n),I.gc().addTaskClicked()})),I.Ub(2,"mat-icon"),I.Bc(3,"add"),I.Tb(),I.Ub(4,"span"),I.Bc(5," Add Task "),I.Tb(),I.Tb(),I.Rb()}}function jt(t,e){if(1&t){var n=I.Vb();I.Ub(0,"div",5,6),I.Ub(2,"mat-icon"),I.Bc(3,"drag_indicator"),I.Tb(),I.Pb(4,"mat-checkbox",7),I.Ub(5,"form",8),I.cc("ngSubmit",(function(){return I.tc(n),I.gc().onSubmit()})),I.Pb(6,"input",9,10),I.Tb(),I.Tb()}if(2&t){var a=I.gc();I.Cb(5),I.mc("formGroup",a.taskGroup)}}var vt,Pt=((vt=function(){function t(e,n){_classCallCheck(this,t),this.store=e,this.cd=n,this.status=c.New,this.editActive=!1,this.taskGroup=new o.e({taskName:new o.c}),this.onDestroy$=new ct.a}return _createClass(t,[{key:"ngOnInit",value:function(){var t=this;this.currentWorkspace$=this.store.pipe(Object(s.t)(w.a)),this.currentProject$=this.store.pipe(Object(s.t)(x.b)),this.currentUser$=this.store.pipe(Object(s.t)(rt.d)),this.inlineEditActive$=this.store.pipe(Object(s.t)(et),Object(dt.a)()),this.outSideClickListener$=Object(ot.a)(document,"mousedown",{passive:!0}).pipe(Object(ut.a)(this.onDestroy$),Object(bt.a)(200),Object(pt.a)((function(e){!t.editActive||t.containerElementRef.nativeElement.contains(e.target)||t.formElementRef.nativeElement.contains(e.target)||(t.editActive=!1,t.store.dispatch(T({active:!1})),t.outsideClickSubscription.unsubscribe(),t.cd.detectChanges())})))}},{key:"ngOnDestroy",value:function(){this.onDestroy$.next(),this.onDestroy$.complete()}},{key:"addTaskClicked",value:function(){this.editActive=!0,this.store.dispatch(T({active:!0})),this.outsideClickSubscription=this.outSideClickListener$.subscribe(),this.cd.detectChanges(),this.inputElementRef.nativeElement.focus()}},{key:"onSubmit",value:function(){var t=this;Object(lt.a)([this.currentWorkspace$,this.currentProject$,this.currentUser$]).pipe(Object(gt.a)()).subscribe({next:function(e){var n=_slicedToArray(e,3),a=n[0],i=n[1],s=n[2];return t.createTask(a,i,s)}})}},{key:"createTask",value:function(t,e,n){var a=this.siblings&&this.siblings[this.siblings.length-1];this.store.dispatch(g({task:{name:this.taskName.value,workspace:t.slug,projectId:e.id,status:this.status,sortOrder:a&&a.sortOrder+1||1,assigneeId:n.userId}})),this.taskGroup.reset()}},{key:"taskName",get:function(){return this.taskGroup.get("taskName")}}]),t}()).\u0275fac=function(t){return new(t||vt)(I.Ob(s.h),I.Ob(I.h))},vt.\u0275cmp=I.Ib({type:vt,selectors:[["app-task-inline"]],viewQuery:function(t,e){var n;1&t&&(I.Fc(ht,!0),I.Fc(ft,!0),I.Fc(Ot,!0)),2&t&&(I.qc(n=I.dc())&&(e.containerElementRef=n.first),I.qc(n=I.dc())&&(e.formElementRef=n.first),I.qc(n=I.dc())&&(e.inputElementRef=n.first))},inputs:{status:"status",siblings:"siblings"},decls:5,vars:4,consts:[[1,"task-inline-container"],["taskInlineContainer",""],[4,"ngIf","ngIfElse"],["elseTemplate",""],["mat-button","","disableRipple","true",1,"new-task-button",3,"click"],[1,"inline-task-form"],["taskInlineForm",""],["color","primary","disabled",""],[1,"task-form",3,"formGroup","ngSubmit"],["matInput","","formControlName","taskName","placeholder","What do you need to get done?",1,"inline-task-input"],["taskInput",""]],template:function(t,e){if(1&t&&(I.Ub(0,"div",0,1),I.zc(2,Ct,6,0,"ng-container",2),I.zc(3,jt,8,1,"ng-template",null,3,I.Ac),I.Tb()),2&t){var n=I.rc(4);I.Fb("edit-mode",e.editActive),I.Cb(2),I.mc("ngIf",!e.editActive)("ngIfElse",n)}},directives:[$.l,E.b,kt.a,mt.a,o.r,o.l,o.f,D.b,o.b,o.k,o.d],styles:[".task-inline-container[_ngcontent-%COMP%]{width:100%;min-height:40px;max-height:40px;display:flex;flex-direction:row;justify-content:center}.task-inline-container[_ngcontent-%COMP%]   .new-task-button[_ngcontent-%COMP%]{width:100%;display:flex;justify-content:flex-start;transition:background-color .2s ease-in;flex-direction:row;background-color:transparent;border:0;text-align:start;font-family:inherit;font-weight:500;font-size:.8rem;letter-spacing:.125px;padding:0 2.3rem;border-radius:0}.task-inline-container[_ngcontent-%COMP%]   .new-task-button[_ngcontent-%COMP%]   span[_ngcontent-%COMP%]{margin:auto 1rem}.task-inline-container[_ngcontent-%COMP%]   .inline-task-form[_ngcontent-%COMP%]{width:100%;display:flex;flex-direction:row}.task-inline-container[_ngcontent-%COMP%]   .inline-task-form[_ngcontent-%COMP%]   mat-icon[_ngcontent-%COMP%]{padding:.5rem}.task-inline-container[_ngcontent-%COMP%]   .inline-task-form[_ngcontent-%COMP%]   mat-checkbox[_ngcontent-%COMP%]{padding:.5rem 0}.task-inline-container[_ngcontent-%COMP%]   .inline-task-form[_ngcontent-%COMP%]   .task-form[_ngcontent-%COMP%]{height:100%;width:100%;padding:0;display:flex;flex-direction:row;justify-content:space-around}.task-inline-container[_ngcontent-%COMP%]   .inline-task-form[_ngcontent-%COMP%]   .inline-task-input[_ngcontent-%COMP%]{width:100%;font-size:14px;font-family:inherit;border:0;background-color:transparent;padding:.2rem 1.2rem}"]}),vt),yt=n("Rn6e"),Tt=n("jHLV"),_t=n("ZFy/"),Mt=n("rJgo"),wt=n("f44v"),xt=n("UTQ3"),Ut=["app-task-list-item",""];function It(t,e){if(1&t){var n=I.Vb();I.Ub(0,"button",14),I.cc("click",(function(){return I.tc(n),I.gc().markCompleteClicked()})),I.Ub(1,"mat-icon"),I.Bc(2,"done"),I.Tb(),I.Ub(3,"span"),I.Bc(4,"Mark Complete"),I.Tb(),I.Tb(),I.Ub(5,"button",14),I.cc("click",(function(){return I.tc(n),I.gc().moveToBacklogClicked()})),I.Ub(6,"mat-icon"),I.Bc(7,"assignment_returned_outline"),I.Tb(),I.Ub(8,"span"),I.Bc(9,"Move to Backlog"),I.Tb(),I.Tb(),I.Ub(10,"button",14),I.cc("click",(function(){return I.tc(n),I.gc().deleteClicked()})),I.Ub(11,"mat-icon"),I.Bc(12,"delete"),I.Tb(),I.Ub(13,"span"),I.Bc(14,"Delete"),I.Tb(),I.Tb()}}var St,Dt=((St=function(){function t(e,n){_classCallCheck(this,t),this.store=e,this.dialog=n,this.checked=!1}return _createClass(t,[{key:"titleClicked",value:function(){this.store.dispatch(P({task:this.task}))}},{key:"editClicked",value:function(){this.store.dispatch(h({task:this.task}))}},{key:"deleteClicked",value:function(){var t=this;this.dialog.open(yt.a,{data:{title:"Are you sure you want to delete task?",content:"Delete task - ".concat(Tt.a.truncate(this.task.name)),confirm:"Delete"}}).afterClosed().subscribe((function(e){e&&t.store.dispatch(C({task:t.task}))}))}},{key:"markCompleteClicked",value:function(){this.store.dispatch(h({task:Object.assign(Object.assign({},this.task),{status:c.Complete})}))}},{key:"moveToBacklogClicked",value:function(){this.store.dispatch(h({task:Object.assign(Object.assign({},this.task),{status:c.InActive})}))}}]),t}()).\u0275fac=function(t){return new(t||St)(I.Ob(s.h),I.Ob(l.b))},St.\u0275cmp=I.Ib({type:St,selectors:[["","app-task-list-item",""]],inputs:{task:"task"},attrs:Ut,decls:19,vars:8,consts:[[1,"task-card"],[1,"task-header"],[1,"task-header-title"],["cdkDragHandle","","mat-icon-button","","aria-label","more","matTooltip","click for more options. click and hold to drag task",3,"matTooltipShowDelay","matMenuTriggerFor"],["color","primary",3,"ngModel","ngModelChange"],[1,"title",3,"click"],[1,"task-footer"],[1,"chip-list"],["ariaOrientation","horizontal"],["color","primary"],[1,"chip-spacer"],["size","24","textSizeRatio","2",1,"task-card-user-chip",3,"name"],["menu","matMenu"],["matMenuContent",""],["mat-menu-item","",3,"click"]],template:function(t,e){if(1&t&&(I.Ub(0,"div",0),I.Ub(1,"div",1),I.Ub(2,"div",2),I.Ub(3,"button",3),I.Ub(4,"mat-icon"),I.Bc(5,"drag_indicator"),I.Tb(),I.Tb(),I.Ub(6,"mat-checkbox",4),I.cc("ngModelChange",(function(t){return e.checked=t})),I.Tb(),I.Ub(7,"div",5),I.cc("click",(function(){return e.titleClicked()})),I.Bc(8),I.Tb(),I.Tb(),I.Ub(9,"div",6),I.Ub(10,"div",7),I.Ub(11,"mat-chip-list",8),I.Ub(12,"mat-chip",9),I.Bc(13),I.Tb(),I.Pb(14,"div",10),I.Tb(),I.Tb(),I.Pb(15,"ngx-avatar",11),I.Ub(16,"mat-menu",null,12),I.zc(18,It,15,0,"ng-template",13),I.Tb(),I.Tb(),I.Tb(),I.Tb()),2&t){var n=I.rc(17);I.Fb("task-item-selected",e.checked),I.Cb(3),I.mc("matTooltipShowDelay",1200)("matMenuTriggerFor",n),I.Cb(3),I.mc("ngModel",e.checked),I.Cb(2),I.Dc(" ",e.task.name," "),I.Cb(5),I.Dc(" ",e.task.projectName," "),I.Cb(2),I.mc("name",e.task.ownerUsername)}},directives:[E.b,it.b,_t.a,Mt.d,kt.a,mt.a,o.k,o.n,wt.b,wt.a,xt.a,Mt.e,Mt.a,Mt.b],styles:['.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]{display:flex;flex-direction:row;justify-content:space-between;align-items:center}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .card-chips[_ngcontent-%COMP%]{justify-content:flex-end}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .title[_ngcontent-%COMP%]{margin-left:1.2rem;font-size:14px;cursor:pointer;white-space:nowrap}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .input-group-prepend[_ngcontent-%COMP%]   label[_ngcontent-%COMP%], .task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .input-group-prepend[_ngcontent-%COMP%]   span[_ngcontent-%COMP%]{width:10rem}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .fas[_ngcontent-%COMP%]{margin-right:1.4rem}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   mat-divider[_ngcontent-%COMP%]{margin-top:2em;margin-bottom:2em}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .task-header-title[_ngcontent-%COMP%]{display:flex;flex-direction:row;justify-content:flex-start;margin-top:auto;margin-bottom:auto;align-items:center}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .task-header-title[_ngcontent-%COMP%]   .mat-checkbox[_ngcontent-%COMP%]{margin:auto}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .task-footer[_ngcontent-%COMP%]{display:flex;flex-direction:row;align-items:center;justify-content:flex-end}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .chip-list[_ngcontent-%COMP%]{display:flex;flex-direction:column;justify-content:center;height:100%;margin-right:.4rem}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .chip-spacer[_ngcontent-%COMP%]{width:.4rem}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .mat-standard-chip[_ngcontent-%COMP%]{min-height:24px!important}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .expand-icon[_ngcontent-%COMP%]{width:24px;height:24px;line-height:24px;margin-left:.4rem;visibility:hidden}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]   .task-card-user-chip[_ngcontent-%COMP%]{margin-right:.6rem;font-size:12px/24px "Roboto",sans-serif!important}.task-card[_ngcontent-%COMP%]   .task-header[_ngcontent-%COMP%]:hover   .expand-icon[_ngcontent-%COMP%]{visibility:visible}@media only screen and (max-width:900px){.task-header[_ngcontent-%COMP%]{flex-direction:column}.chip-list[_ngcontent-%COMP%]{display:none}}@media only screen and (max-width:600px){.mat-card[_ngcontent-%COMP%]:not([class*=mat-elevation-z]){background-color:transparent!important;border:0!important;box-shadow:none!important}}'],data:{animation:[R.d]}}),St);function At(t,e){if(1&t&&I.Pb(0,"div",7),2&t){var n=e.$implicit;I.mc("task",n)("cdkDragData",n)}}var Ft=function(t,e){return{tasks:t,status:e}};function $t(t,e){if(1&t){var n=I.Vb();I.Sb(0),I.Ub(1,"div",4),I.cc("cdkDropListDropped",(function(t){return I.tc(n),I.gc().drop(t)})),I.zc(2,At,1,2,"div",5),I.Pb(3,"app-task-inline",6),I.Tb(),I.Rb()}if(2&t){var a=I.gc();I.Cb(1),I.mc("cdkDropListData",I.pc(4,Ft,a.tasks,a.status)),I.Cb(1),I.mc("ngForOf",a.tasks),I.Cb(1),I.mc("status",a.status)("siblings",a.tasks)}}function Et(t,e){if(1&t&&(I.Sb(0),I.Pb(1,"app-task-inline",9),I.Ub(2,"div",10),I.Ub(3,"div",11),I.Pb(4,"i",12),I.Ub(5,"h4",13),I.Bc(6),I.Tb(),I.Tb(),I.Tb(),I.Rb()),2&t){var n=I.gc(2);I.Cb(1),I.mc("status",n.status),I.Cb(5),I.Dc(" ",n.emptyMessage," ")}}function Bt(t,e){1&t&&(I.Ub(0,"div",14),I.Pb(1,"div",15),I.Tb())}function Nt(t,e){if(1&t&&(I.zc(0,Et,7,2,"ng-container",2),I.hc(1,"async"),I.zc(2,Bt,2,0,"ng-template",null,8,I.Ac)),2&t){var n=I.rc(3),a=I.gc();I.mc("ngIf",I.ic(1,2,a.loaded))("ngIfElse",n)}}var zt,Lt=((zt=function(){function t(e){_classCallCheck(this,t),this.store=e}return _createClass(t,[{key:"ngOnInit",value:function(){}},{key:"drop",value:function(t){t.previousContainer===t.container?Object(it.f)(t.container.data.tasks,t.previousIndex,t.currentIndex):Object(it.g)(t.previousContainer.data.tasks,t.container.data.tasks,t.previousIndex,t.currentIndex);var e=t.container.data.tasks,n=e[t.currentIndex-1],a=e[t.currentIndex+1],i=n&&n.sortOrder,s=a&&a.sortOrder,r=Object(st.a)(i,s),c=t.container.data.status,o=t.item.data;o.sortOrder===r&&o.status===c||this.moveTask(o,c,r)}},{key:"moveTask",value:function(t,e,n){this.store.dispatch(h({task:Object.assign(Object.assign({},t),{status:e,sortOrder:n})}))}}]),t}()).\u0275fac=function(t){return new(t||zt)(I.Ob(s.h))},zt.\u0275cmp=I.Ib({type:zt,selectors:[["app-task-list-group"]],inputs:{groupName:"groupName",tasks:"tasks",header:"header",emptyMessage:"emptyMessage",loaded:"loaded",status:"status"},decls:8,vars:5,consts:[[1,"tasks-container"],[1,"task-list"],[4,"ngIf","ngIfElse"],["elseTemplate",""],["cdkDropList","",1,"task-list-group",3,"cdkDropListData","cdkDropListDropped"],["cdkDrag","","class","task-drag","app-task-list-item","",3,"task","cdkDragData",4,"ngFor","ngForOf"],[3,"status","siblings"],["cdkDrag","","app-task-list-item","",1,"task-drag",3,"task","cdkDragData"],["loadedElseTemplate",""],[3,"status"],[1,"list-empty"],[1,"list-empty-mark"],[1,"far","fa-compass"],[1,"empty-text"],[1,"spinner-container"],[1,"typing-loader"]],template:function(t,e){if(1&t&&(I.Ub(0,"div",0),I.Ub(1,"h4"),I.Bc(2),I.Tb(),I.Ub(3,"div",1),I.zc(4,$t,4,7,"ng-container",2),I.hc(5,"async"),I.zc(6,Nt,4,4,"ng-template",null,3,I.Ac),I.Tb(),I.Tb()),2&t){var n=I.rc(7);I.Cb(2),I.Dc(" ",e.header," "),I.Cb(2),I.mc("ngIf",I.ic(5,3,e.loaded)&&e.tasks.length)("ngIfElse",n)}},directives:[$.l,it.c,$.k,Pt,it.a,Dt],pipes:[$.b],styles:[".tasks-container[_ngcontent-%COMP%]{transition:all .6s}.task-list[_ngcontent-%COMP%]{min-height:196px;overflow:hidden;display:flex;flex-direction:column}.task-list-group[_ngcontent-%COMP%]{min-height:4rem}@media (min-width:600px){.task-list[_ngcontent-%COMP%]{min-height:196px;border-radius:.2rem;overflow:hidden;display:block}h4[_ngcontent-%COMP%]{margin-left:0}}.cdk-drag-preview[_ngcontent-%COMP%]{box-sizing:border-box;border-radius:.4rem;overflow:hidden;box-shadow:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12)}.cdk-drag-placeholder[_ngcontent-%COMP%]{opacity:.2}.cdk-drag-animating[_ngcontent-%COMP%]{transition:transform .25s cubic-bezier(.24,0,.2,1)}.task-drag[_ngcontent-%COMP%]:last-child{border:0}.task-list[_ngcontent-%COMP%]   .cdk-drop-list-dragging[_ngcontent-%COMP%]   .task-drag[_ngcontent-%COMP%]:not(.cdk-drag-placeholder){transition:transform .25s cubic-bezier(.24,0,.2,1)}.list-empty[_ngcontent-%COMP%], .list-empty[_ngcontent-%COMP%]   .list-empty-mark[_ngcontent-%COMP%]{display:flex;justify-content:center}.list-empty[_ngcontent-%COMP%]   .list-empty-mark[_ngcontent-%COMP%]{font-size:3rem;flex-direction:column;height:100%}.list-empty[_ngcontent-%COMP%]   .list-empty-mark[_ngcontent-%COMP%]   .far[_ngcontent-%COMP%]{margin:1.4rem auto;font-size:6rem}.list-empty[_ngcontent-%COMP%]   .list-empty-mark[_ngcontent-%COMP%]   .empty-text[_ngcontent-%COMP%]{color:grey;margin:0 4rem 3rem}.spinner-container[_ngcontent-%COMP%]{padding:4.6rem}"],data:{animation:[R.b,R.a]}}),zt);function Rt(t,e){if(1&t){var n=I.Vb();I.Ub(0,"div",1),I.Ub(1,"h4",2),I.Bc(2," Active Task "),I.Tb(),I.Ub(3,"div",3),I.Ub(4,"div",4),I.Ub(5,"h5"),I.Bc(6),I.Tb(),I.Ub(7,"button",5),I.cc("click",(function(){return I.tc(n),I.gc().closeClicked()})),I.Ub(8,"mat-icon"),I.Bc(9,"close"),I.Tb(),I.Tb(),I.Tb(),I.Ub(10,"div",6),I.Ub(11,"p"),I.Bc(12),I.Tb(),I.Ub(13,"mat-chip-list",7),I.Ub(14,"mat-chip",8),I.Bc(15),I.Tb(),I.Pb(16,"div",9),I.Ub(17,"mat-chip",10),I.Bc(18),I.Tb(),I.Tb(),I.Tb(),I.Tb(),I.Tb()}if(2&t){var a=e.ngIf;I.Cb(6),I.Cc(a.name),I.Cb(6),I.Dc(" ",a.description," "),I.Cb(3),I.Dc(" ",a.projectName," "),I.Cb(3),I.Dc(" ",a.assigneeUsername," ")}}var Gt,qt=((Gt=function(){function t(e){_classCallCheck(this,t),this.store=e,this.$task=this.store.select(tt)}return _createClass(t,[{key:"ngOnInit",value:function(){}},{key:"closeClicked",value:function(){this.store.dispatch(y())}}]),t}()).\u0275fac=function(t){return new(t||Gt)(I.Ob(s.h))},Gt.\u0275cmp=I.Ib({type:Gt,selectors:[["app-task-detail"]],decls:2,vars:3,consts:[["class","tasks-container",4,"ngIf"],[1,"tasks-container"],[1,"task-detail-header","text-ellipsis","w-100"],[1,"task-list","mb-5"],[1,"task-title-bar"],["mat-icon-button","","aria-label","Close task detail panel",3,"click"],[1,"task-body"],["ariaOrientation","horizontal"],["color","primary"],[1,"chip-spacer"],["color","accent"]],template:function(t,e){1&t&&(I.zc(0,Rt,19,4,"div",0),I.hc(1,"async")),2&t&&I.mc("ngIf",I.ic(1,1,e.$task))},directives:[$.l,E.b,kt.a,wt.b,wt.a],pipes:[$.b],styles:[".tasks-container[_ngcontent-%COMP%]{height:100%;display:flex;flex-direction:column}.task-list[_ngcontent-%COMP%]{flex:1;min-height:83.28vh}.task-detail-header[_ngcontent-%COMP%]{overflow:visible;line-height:1rem}.task-title-bar[_ngcontent-%COMP%]{display:flex;flex-direction:row;justify-content:space-between;padding:.4rem}.task-title-bar[_ngcontent-%COMP%]   h5[_ngcontent-%COMP%]{margin:auto 1.4rem;font-size:1rem;font-weight:500;line-height:1rem;overflow:visible}.task-body[_ngcontent-%COMP%]{padding:0 1.6rem}"]}),Gt);function Wt(t,e){if(1&t&&(I.Pb(0,"app-task-list-group",6),I.hc(1,"async")),2&t){var n=e.$implicit,a=I.gc();I.mc("tasks",I.ic(1,6,n.tasks))("header",n.header)("groupName",n.groupName)("status",n.status)("emptyMessage",n.emptyMessage)("loaded",a.loaded$)}}function Vt(t,e){1&t&&(I.Ub(0,"div",7),I.Pb(1,"app-task-detail"),I.Tb())}var Yt,Ht,Jt,Qt,Zt=[{path:"**",component:(Yt=function(){function t(e,n,a){_classCallCheck(this,t),this.snackBar=e,this.dialog=n,this.store=a,this.myTasks$=this.store.pipe(Object(s.t)(Z)),this.completedTasks$=this.store.pipe(Object(s.t)(Q)),this.backlogTasks$=this.store.pipe(Object(s.t)(X)),this.loaded$=this.store.pipe(Object(s.t)(K)),this.selectedTask$=this.store.pipe(Object(s.t)(tt)),this.taskGroups=[{groupName:"my-tasks",tasks:this.myTasks$,header:"My Tasks",status:c.New,emptyMessage:"You have no tasks. Click the button in the bottom right to create a task."},{groupName:"completed-tasks",tasks:this.completedTasks$,header:"Completed Tasks",status:c.Complete,emptyMessage:"You currently have no completed tasks. Mark a task as completed and it will show up here."},{groupName:"backlog-tasks",tasks:this.backlogTasks$,header:"Backlog",status:c.InActive,emptyMessage:"Your backlog is currently empty hurray!"}]}return _createClass(t,[{key:"ngOnInit",value:function(){this.store.dispatch(u())}},{key:"showAddModal",value:function(){this.dialog.open(L,{width:"600px"}).afterClosed().subscribe((function(){}))}}]),t}(),Yt.\u0275fac=function(t){return new(t||Yt)(I.Ob(nt.b),I.Ob(l.b),I.Ob(s.h))},Yt.\u0275cmp=I.Ib({type:Yt,selectors:[["app-project-tasks"]],decls:8,vars:9,consts:[[3,"verticalPadding"],[1,"task-list-view","row"],[1,"task-list-main"],["cdkDropListGroup","",1,"drag-group"],["class","mb-5",3,"tasks","header","groupName","status","emptyMessage","loaded",4,"ngFor","ngForOf"],["class","task-detail-aside",4,"ngIf"],[1,"mb-5",3,"tasks","header","groupName","status","emptyMessage","loaded"],[1,"task-detail-aside"]],template:function(t,e){1&t&&(I.Ub(0,"app-page-container",0),I.Ub(1,"div",1),I.Ub(2,"div",2),I.hc(3,"async"),I.Ub(4,"div",3),I.zc(5,Wt,2,8,"app-task-list-group",4),I.Tb(),I.Tb(),I.zc(6,Vt,2,0,"div",5),I.hc(7,"async"),I.Tb(),I.Tb()),2&t&&(I.mc("verticalPadding",!1),I.Cb(2),I.Fb("task-list-shrink",I.ic(3,5,e.selectedTask$)),I.Cb(3),I.mc("ngForOf",e.taskGroups),I.Cb(1),I.mc("ngIf",I.ic(7,7,e.selectedTask$)))},directives:[at.a,it.d,$.k,$.l,Lt,qt],pipes:[$.b],styles:[".container[_ngcontent-%COMP%]{width:100%;padding-right:0;padding-left:0;margin-right:auto;margin-left:auto}.fas[_ngcontent-%COMP%]{margin-right:1rem}.projects-header[_ngcontent-%COMP%]{display:flex;justify-content:space-between;flex-direction:row;margin-bottom:2rem}.icon[_ngcontent-%COMP%]{text-align:center;height:48px;width:48px;font-size:2.5rem;padding:.5rem;display:inline-block;margin:0 1.5rem 0 auto}.action-button[_ngcontent-%COMP%]{margin-right:.8rem}.action-button[_ngcontent-%COMP%]:first-child, .mat-card-actions[_ngcontent-%COMP%]   .mat-raised-button[_ngcontent-%COMP%]:first-child{margin-left:0;margin-right:.8rem!important}.action-mat-icon[_ngcontent-%COMP%]{margin-right:.6rem}.task-list-shrink[_ngcontent-%COMP%]{margin-right:660px}.task-detail-aside[_ngcontent-%COMP%]{position:fixed;width:640px;right:50px;top:72px}.task-list-main[_ngcontent-%COMP%]{width:100%}.task-list-view[_ngcontent-%COMP%]{padding-bottom:24rem}"],data:{animation:[R.b,R.a]}}),Yt)}],Xt=((Ht=function t(){_classCallCheck(this,t)}).\u0275mod=I.Mb({type:Ht}),Ht.\u0275inj=I.Lb({factory:function(t){return new(t||Ht)},imports:[[r.i.forChild(Zt)],r.i]}),Ht),Kt=n("LRne"),te=n("zp1y"),ee=n("eIep"),ne=n("lJxs"),ae=n("JIr8"),ie=n("AytR"),se=n("IheW"),re=((Jt=function(){function t(e){_classCallCheck(this,t),this.http=e}return _createClass(t,[{key:"get",value:function(t){return this.http.get(ie.a.apiEndpoint+"api/ProjectTasks?workspaceSlug=".concat(t))}},{key:"post",value:function(t){return this.http.post(ie.a.apiEndpoint+"api/ProjectTasks",t)}},{key:"put",value:function(t){return this.http.put(ie.a.apiEndpoint+"api/ProjectTasks",t)}},{key:"delete",value:function(t){return this.http.delete(ie.a.apiEndpoint+"api/ProjectTasks/".concat(t.id))}}]),t}()).\u0275fac=function(t){return new(t||Jt)(I.Yb(se.b))},Jt.\u0275prov=I.Kb({token:Jt,factory:Jt.\u0275fac,providedIn:"root"}),Jt),ce=n("lc/E"),oe=((Qt=function t(e,n,a,s){var r=this;_classCallCheck(this,t),this.actions$=e,this.projectTasksService=n,this.snackbar=a,this.store=s,this.loadProjectTasks$=Object(i.d)((function(){return r.actions$.pipe(Object(i.e)(u),Object(te.a)(r.store.select(w.a)),Object(ee.a)((function(t){var e=_slicedToArray(t,2),n=(e[0],e[1]);return r.projectTasksService.get(n.slug).pipe(Object(ne.a)((function(t){return b({tasks:t})})),Object(ae.a)((function(t){return Object(Kt.a)(p(t))})))})))})),this.createProjectTask$=Object(i.d)((function(){return r.actions$.pipe(Object(i.e)(g),Object(ee.a)((function(t){return r.projectTasksService.post(t.task).pipe(Object(pt.a)((function(){return r.snackbar.open("Task created")})),Object(ne.a)((function(t){return k({task:t})})),Object(ae.a)((function(t){return Object(Kt.a)(m({error:t}))})))})))})),this.editProjectTask$=Object(i.d)((function(){return r.actions$.pipe(Object(i.e)(h),Object(ee.a)((function(t){return r.projectTasksService.put(t.task).pipe(Object(pt.a)((function(){return!!t.silent&&r.snackbar.open("Task updated")})),Object(ne.a)((function(t){return f({task:t})})),Object(ae.a)((function(t){return Object(Kt.a)(O({error:t}))})))})))})),this.deleteProjectTask$=Object(i.d)((function(){return r.actions$.pipe(Object(i.e)(C),Object(ee.a)((function(t){return r.projectTasksService.delete(t.task).pipe(Object(pt.a)((function(){return r.snackbar.open("Task deleted")})),Object(ne.a)((function(t){return j({task:t})})),Object(ae.a)((function(t){return Object(Kt.a)(v({error:t}))})))})))})),this.onWorkspaceSelected$=Object(i.d)((function(){return r.actions$.pipe(Object(i.e)(ce.m),Object(ne.a)(d))}))}).\u0275fac=function(t){return new(t||Qt)(I.Yb(i.a),I.Yb(re),I.Yb(nt.b),I.Yb(s.h))},Qt.\u0275prov=I.Kb({token:Qt,factory:Qt.\u0275fac}),Qt),le=Object(s.p)(V,Object(s.r)(d,(function(){return V})),Object(s.r)(u,(function(t){return Object.assign(Object.assign({},t),{loading:!0})})),Object(s.r)(p,(function(t,e){var n=e.error;return Object.assign(Object.assign({},t),{loading:!1,loadProjectsError:n})})),Object(s.r)(b,(function(t,e){var n=e.tasks;return W.setAll(n,Object.assign(Object.assign({},t),{loading:!1,loaded:!0}))})),Object(s.r)(g,(function(t){return Object.assign(Object.assign({},t),{loadingNewTask:!0})})),Object(s.r)(m,(function(t,e){var n=e.error;return Object.assign(Object.assign({},t),{loadingNewTask:!1,createNewTaskError:n})})),Object(s.r)(k,(function(t,e){var n=e.task;return W.addOne(n,Object.assign(Object.assign({},t),{loadingNewTask:!1,createdTask:n}))})),Object(s.r)(h,(function(t){return Object.assign(Object.assign({},t),{editState:{loading:!0}})})),Object(s.r)(O,(function(t,e){var n=e.error;return Object.assign(Object.assign({},t),{editState:{loading:!1,error:n}})})),Object(s.r)(f,(function(t,e){var n=e.task;return W.upsertOne(n,Object.assign(Object.assign({},t),{editState:{loading:!1}}))})),Object(s.r)(C,(function(t){return Object.assign(Object.assign({},t),{deleteState:{loading:!0}})})),Object(s.r)(v,(function(t,e){var n=e.error;return Object.assign(Object.assign({},t),{deleteState:{loading:!1,error:n}})})),Object(s.r)(j,(function(t,e){var n=e.task;return W.removeOne(n.id,Object.assign(Object.assign({},t),{deleteState:{loading:!1}}))})),Object(s.r)(P,(function(t,e){var n=e.task;return Object.assign(Object.assign({},t),{selectedTask:n})})),Object(s.r)(y,(function(t){return Object.assign(Object.assign({},t),{selectedTask:void 0})})),Object(s.r)(T,(function(t,e){var n=e.active;return Object.assign(Object.assign({},t),{inlineEditActive:n})})));function de(t,e){return le(t,e)}var ue,be=n("Fk/C"),pe=((ue=function t(){_classCallCheck(this,t)}).\u0275mod=I.Mb({type:ue}),ue.\u0275inj=I.Lb({factory:function(t){return new(t||ue)},imports:[[a.a,be.a,s.j.forFeature("tasks",de),i.c.forFeature([oe]),Xt]]}),ue)}}]);