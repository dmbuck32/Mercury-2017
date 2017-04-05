a = 1; %Sensitivity
x = -1:.05:1; %Input
v = 100; %max velocity

relativeSpeed = (exp(a.*x)-1)./(exp(a.*x)+1);
relativeSpeed2 = .5*x.^3;
relativeSpeed3 = 500*x.^3 + 1500;

%plot(x, relativeSpeed);
hold on;
%plot(x, relativeSpeed2);
plot(x, relativeSpeed3);
grid on;

driveSpeed = v .* relativeSpeed;