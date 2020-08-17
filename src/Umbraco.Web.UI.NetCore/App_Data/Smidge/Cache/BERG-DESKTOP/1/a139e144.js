/**
 * Angular Dynamic Locale - 0.1.37
 * https://github.com/lgalfaso/angular-dynamic-locale
 * License: MIT
 */

!function(e,t){"function"==typeof define&&define.amd?define([],function(){return t()}):"object"==typeof exports?module.exports=t():t()}(0,function(){"use strict";return angular.module("tmh.dynamicLocale",[]).config(["$provide",function(e){function t(e){return e.$stateful=!0,e}e.decorator("dateFilter",["$delegate",t]),e.decorator("numberFilter",["$delegate",t]),e.decorator("currencyFilter",["$delegate",t])}]).constant("tmhDynamicLocale.STORAGE_KEY","tmhDynamicLocale.locale").provider("tmhDynamicLocale",["tmhDynamicLocale.STORAGE_KEY",function(e){var u,p,$,v,s="angular/i18n/angular-locale_{{locale}}.js",d="tmhDynamicLocaleStorageCache",L=e,f="get",S="put",C={},g={};function h(e,t,o,n,a,r,c){function i(n,a){v===o&&(angular.forEach(n,function(e,t){a[t]?angular.isArray(a[t])&&(n[t].length=a[t].length):delete n[t]}),angular.forEach(a,function(e,t){angular.isArray(a[t])||angular.isObject(a[t])?(n[t]||(n[t]=angular.isArray(a[t])?[]:{}),i(n[t],a[t])):n[t]=a[t]}))}if(C[o])return C[v=o];var l,u,s,d,f,g,h,m,y=a.defer();return o===v?y.resolve(t):(l=r.get(o))?(v=o,n.$evalAsync(function(){i(t,l),$[S](L,o),n.$broadcast("$localeChangeSuccess",o,t),y.resolve(t)})):(C[v=o]=y.promise,u=e,s=function(){var e=angular.injector(["ngLocale"]).get("$locale");i(t,e),r.put(o,e),delete C[o],n.$applyAsync(function(){$[S](L,o),n.$broadcast("$localeChangeSuccess",o,t),y.resolve(t)})},d=function(){delete C[o],n.$applyAsync(function(){v===o&&(v=t.id),n.$broadcast("$localeChangeError",o),y.reject(o)})},f=c,g=document.createElement("script"),h=p||document.getElementsByTagName("body")[0],m=!1,g.type="text/javascript",g.readyState?g.onreadystatechange=function(){"complete"!==g.readyState&&"loaded"!==g.readyState||(g.onreadystatechange=null,f(function(){m||(m=!0,g.parentNode===h&&h.removeChild(g),s())},30,!1))}:(g.onload=function(){m||(m=!0,g.parentNode===h&&h.removeChild(g),s())},g.onerror=function(){m||(m=!0,g.parentNode===h&&h.removeChild(g),d())}),g.src=u,g.async=!0,h.appendChild(g)),y.promise}this.localeLocationPattern=function(e){return e?(s=e,this):s},this.appendScriptTo=function(e){p=e},this.useStorage=function(e){d=e,f="get",S="put"},this.useCookieStorage=function(){angular.version.minor<7?this.useStorage("$cookieStore"):(this.useStorage("$cookies"),f="getObject",S="putObject")},this.defaultLocale=function(e){u=e},this.storageKey=function(e){return e?(L=e,this):L},this.addLocalePatternValue=function(e,t){g[e]=t},this.$get=["$rootScope","$injector","$interpolate","$locale","$q","tmhDynamicLocaleCache","$timeout",function(n,e,t,a,o,r,c){var i=t(s);return $=e.get(d),n.$evalAsync(function(){var e;(e=$[f](L)||u)&&l(e)}),{set:l,get:function(){return v}};function l(e){var t={locale:e,angularVersion:angular.version.full};return h(i(angular.extend({},g,t)),a,e,n,o,r,c)}}]}]).provider("tmhDynamicLocaleCache",function(){this.$get=["$cacheFactory",function(e){return e("tmh.dynamicLocales")}]}).provider("tmhDynamicLocaleStorageCache",function(){this.$get=["$cacheFactory",function(e){return e("tmh.dynamicLocales.store")}]}).run(["tmhDynamicLocale",angular.noop]),"tmh.dynamicLocale"});
//# sourceMappingURL=tmhDynamicLocale.min.js.map