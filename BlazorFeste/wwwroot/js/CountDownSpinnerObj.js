"use strict";

export var CountDownSpinnerObj = {
  objRef: null,

  startTimer: (htmlDiv, _objRef, time, name) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " CountDownSpinnerObj - startTimer");
    CountDownSpinnerObj.objRef = _objRef;

    var _time = time;
    var htmlElement = document.querySelector(htmlDiv);
    htmlElement.innerText = time;

    setInterval(function () {
      htmlElement.innerText = _time--;
      if (_time == 0) {
        _time = time;
      }
      if (_time == time - 1) {
        CountDownSpinnerObj.objRef.invokeMethodAsync('OnCountdownEnd', name);
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " CountDownSpinnerObj - OnCountdownEnd");
      }
    }, 1000);
  },
};
