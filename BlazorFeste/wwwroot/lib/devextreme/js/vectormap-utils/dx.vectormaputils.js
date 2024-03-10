/*!
* DevExtreme (dx.vectormaputils.js)
* Version: 23.1.8
* Build date: Thu Jan 25 2024
*
* Copyright (c) 2012 - 2024 Developer Express Inc. ALL RIGHTS RESERVED
* Read about DevExtreme licensing here: https://js.devexpress.com/Licensing/
*/
"use strict";!function(n,e){if("function"==typeof define&&define.amd)define((function(n,t,r){e(t)}));else if("object"==typeof module&&module.exports)e(t);else{var t=n.DevExpress=n.DevExpress||{};t=(t=t.viz=t.viz||{}).vectormaputils={},e(t)}}(this,(function(n){function e(){}function t(n){return n}function r(n){return"function"==typeof n}function o(n){var e=new DataView(n),t=0,r={pos:function(){return t},skip:function(n){return t+=n,r},ui8arr:function(n){var e=0,t=[];for(t.length=n;e<n;++e)t[e]=r.ui8();return t},ui8:function(){var n=function(n,e){return n.getUint8(e)}(e,t);return t+=1,n},ui16LE:function(){var n=function(n,e){return n.getUint16(e,!0)}(e,t);return t+=2,n},ui32LE:function(){var n=function(n,e){return n.getUint32(e,!0)}(e,t);return t+=4,n},ui32BE:function(){var n=function(n,e){return n.getUint32(e,!1)}(e,t);return t+=4,n},f64LE:function(){var n=function(n,e){return n.getFloat64(e,!0)}(e,t);return t+=8,n}};return r}function i(n,e,t){var r,i=n[0]?function(n,e){var t,r,o,i,u=[];try{t=new Date,o=function(n){var e={};return e.fileCode=n.ui32BE(),n.skip(20),e.fileLength=n.ui32BE()<<1,e.version=n.ui32LE(),e.type_number=n.ui32LE(),e.type=l[e.type_number],e.bBox_XY=v(n),e.bBox_ZM=m(n,2),e}(n)}catch(n){return void e.push("shp: header parsing error: "+n.message+" / "+n.description)}9994!==o.fileCode&&e.push("shp: file code: "+o.fileCode+" / expected: 9994");1e3!==o.version&&e.push("shp: file version: "+o.version+" / expected: 1000");try{for(;n.pos()<o.fileLength&&(i=P(n,o.type,e));)u.push(i);n.pos()!==o.fileLength&&e.push("shp: file length: "+o.fileLength+" / actual: "+n.pos()),r=new Date}catch(n){e.push("shp: records parsing error: "+n.message+" / "+n.description)}return{bBox:o.bBox_XY,type:o.shapeType,shapes:u,errors:e,time:r-t}}(o(n[0]),t):{},u=n[1]?function(n,e){var t,r,o,i,u;try{t=new Date,o=function(n,e){var t,r,o={versionNumber:n.ui8(),lastUpdate:new Date(1900+n.ui8(),n.ui8()-1,n.ui8()),numberOfRecords:n.ui32LE(),headerLength:n.ui16LE(),recordLength:n.ui16LE(),fields:[]};for(n.skip(20),t=(o.headerLength-n.pos()-1)/32;t>0;--t)o.fields.push(B(n));13!==(r=n.ui8())&&e.push("dbf: header terminator: "+r+" / expected: 13");return o}(n,e),i=function(n,e){var t,r,o=[],i=0,u=n.fields.length,s=0;for(i=0;i<u;++i)r=n.fields[i],(t={name:r.name,parser:E[r.type],length:r.length}).parser||(t.parser=w,e.push("dbf: field "+r.name+" type: "+r.type+" / unknown")),s+=r.length,o.push(t);s+1!==n.recordLength&&e.push("dbf: record length: "+n.recordLength+" / actual: "+(s+1));return o}(o,e),u=function(n,e,t,r,o){var i,u,s,c,a,l=r.length,f=[];for(i=0;i<e;++i){for(c={},s=n.pos(),n.skip(1),u=0;u<l;++u)a=r[u],c[a.name]=a.parser(n,a.length);(s=n.pos()-s)!==t&&o.push("dbf: record #"+(i+1)+" length: "+t+" / actual: "+s),f.push(c)}return f}(n,o.numberOfRecords,o.recordLength,i,e),r=new Date}catch(n){e.push("dbf: parsing error: "+n.message+" / "+n.description)}return{records:u,errors:e,time:r-t}}(o(n[1]),t):{},s=function(n,e,t){var r,o,i=[],u=i.length=Math.max(n.length,e.length);for(r=0;r<u;++r)o=n[r]||{},i[r]={type:"Feature",geometry:{type:o.geoJSON_type||null,coordinates:o.coordinates?t(o.coordinates):[]},properties:e[r]||null};return i}(i.shapes||[],u.records||[],e);return s.length?(r={type:"FeatureCollection",features:s}).bbox=i.bBox:r=null,r}function u(n){return n=n||{},["shp","dbf"].map((function(e){return function(t){var r,o,i;n.substr?(e="."+e,r=n+(n.substr(-e.length).toLowerCase()===e?"":e),o=function(n,e){t(n,e)},(i=new XMLHttpRequest).addEventListener("load",(function(){o(this.response?null:this.statusText,this.response)})),i.open("GET",r),i.responseType="arraybuffer",i.setRequestHeader("X-Requested-With","XMLHttpRequest"),i.send(null)):t(null,n[e]||null)}}))}function s(n,e){var t,r=v(n),o=h(n),i=h(n),u=d(n,o),s=m(n,i),c=[];for(c.length=o,t=0;t<o;++t)c[t]=s.slice(u[t],u[t+1]||i);e.bBox=r,e.coordinates=c}function c(n,e){var t,r,o,i=v(n),u=h(n),s=h(n),c=d(n,u),a=m(n,s),l=y(n),f=g(n,s),p=[];for(p.length=u,t=0;t<u;++t)r=c[t],o=c[t+1]||s,p[t]=b(a.slice(r,o),f.slice(r,o),o-r);e.bBox=i,e.mBox=l,e.coordinates=p}function a(n,e){var t,r,o,i=v(n),u=h(n),s=h(n),c=d(n,u),a=m(n,s),l=y(n),f=g(n,s),p=y(n),b=g(n,s),P=[];for(P.length=u,t=0;t<u;++t)r=c[t],o=c[t+1]||s,P[t]=L(a.slice(r,o),f.slice(r,o),b.slice(r,o),o-r);e.bBox=i,e.zBox=l,e.mBox=p,e.coordinates=P}n.parse=function(n,o,s){var c;return function(n,e){var t=[],r=[],o=1;function i(){0===--o&&e(t,r)}n.forEach((function(n,e){++o,n((function(n,o){t[e]=n,r[e]=o,i()}))})),!1,i()}(u(n),(function(n,u){s=r(o)&&o||r(s)&&s||e,o=!r(o)&&o||{};var a=[];n.forEach((function(n){n&&a.push(n)})),c=i(u,o.precision>=0?function(n){var e=Number("1E"+n);function t(n){return Math.round(n*e)/e}return function n(e){return e.map(e[0].length?n:t)}}(o.precision):t,a),s(c,a.length?a:null)})),c};var l={0:"Null",1:"Point",3:"PolyLine",5:"Polygon",8:"MultiPoint",11:"PointZ",13:"PolyLineZ",15:"PolygonZ",18:"MultiPointZ",21:"PointM",23:"PolyLineM",25:"PolygonM",28:"MultiPointM",31:"MultiPatch"},f={0:e,1:function(n,e){e.coordinates=m(n,1)[0]},3:s,5:s,8:function(n,e){e.bBox=v(n),e.coordinates=m(n,h(n))},11:function(n,e){e.coordinates=m(n,1)[0],e.push(g(n,1)[0],g(n,1)[0])},13:a,15:a,18:function(n,e){var t=v(n),r=h(n),o=m(n,r),i=y(n),u=g(n,r),s=y(n),c=g(n,r);e.bBox=t,e.zBox=i,e.mBox=s,e.coordinates=L(o,u,c,r)},21:function(n,e){e.coordinates=m(n,1)[0],e.coordinates.push(g(n,1)[0])},23:c,25:c,28:function(n,e){var t=v(n),r=h(n),o=m(n,r),i=y(n),u=g(n,r);e.bBox=t,e.mBox=i,e.coordinates=b(o,u,r)},31:function(n,e){var t,r,o,i=v(n),u=h(n),s=h(n),c=d(n,u),a=d(n,u),l=m(n,s),f=y(n),p=g(n,s),b=y(n),P=[];for(P.length=u,t=0;t<u;++t)r=c[t],o=c[t+1]||s,P[t]=L(l.slice(r,o),p.slice(r,o),mValues.slice(r,o),o-r);e.bBox=i,e.zBox=f,e.mBox=b,e.types=a,e.coordinates=P}},p={Null:"Null",Point:"Point",PolyLine:"MultiLineString",Polygon:"Polygon",MultiPoint:"MultiPoint",PointZ:"Point",PolyLineZ:"MultiLineString",PolygonZ:"Polygon",MultiPointZ:"MultiPoint",PointM:"Point",PolyLineM:"MultiLineString",PolygonM:"Polygon",MultiPointM:"MultiPoint",MultiPatch:"MultiPatch"};function h(n){return n.ui32LE()}function d(n,e){var t,r=[];for(r.length=e,t=0;t<e;++t)r[t]=h(n);return r}function g(n,e){var t,r=[];for(r.length=e,t=0;t<e;++t)r[t]=n.f64LE();return r}function v(n){return g(n,4)}function y(n){return[n.f64LE(),n.f64LE()]}function m(n,e){var t,r=[];for(r.length=e,t=0;t<e;++t)r[t]=y(n);return r}function b(n,e,t){var r,o=[];for(o.length=t,r=0;r<t;++r)o[r]=[n[r][0],n[r][1],e[r]];return o}function L(n,e,t,r){var o,i=[];for(i.length=r,o=0;o<r;++o)i[o]=[n[o][0],n[o][1],e[o],t[o]];return i}function P(n,e,t){var r={number:n.ui32BE()},o=n.ui32BE()<<1,i=n.pos(),u=n.ui32LE();return r.type_number=u,r.type=l[u],r.geoJSON_type=p[r.type],r.type?(r.type!==e&&t.push("shp: shape #"+r.number+" type: "+r.type+" / expected: "+e),f[u](n,r),(i=n.pos()-i)!==o&&t.push("shp: shape #"+r.number+" length: "+o+" / actual: "+i)):(t.push("shp: shape #"+r.number+" type: "+u+" / unknown"),r=null),r}var x=String.fromCharCode;function M(n,e){return x.apply(null,n.ui8arr(e))}function B(n){var e={name:M(n,11).replace(/\0*$/gi,""),type:x(n.ui8()),length:n.skip(4).ui8(),count:n.ui8()};return n.skip(14),e}var E={C:function(n,e){var t=M(n,e);try{t=decodeURIComponent(escape(t))}catch(n){}return t.trim()},N:function(n,e){var t=M(n,e);return parseFloat(t)},D:function(n,e){var t=M(n,e);return new Date(t.substring(0,4),t.substring(4,6)-1,t.substring(6,8))}};function w(n,e){return n.skip(e),null}}));
